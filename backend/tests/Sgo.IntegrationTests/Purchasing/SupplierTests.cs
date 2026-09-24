using System.Net;
using System.Net.Http.Json;
using Sgo.Application.Catalog;
using Sgo.Application.Common;
using Sgo.Application.Purchasing;
using Sgo.IntegrationTests.Catalog;
using Sgo.IntegrationTests.Support;

namespace Sgo.IntegrationTests.Purchasing;

internal static class PurchasingTestHelpers
{
    /// <summary>A random, valid legal-entity RFC (tests share one database).</summary>
    public static string NewTaxId()
    {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string alnum = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = Random.Shared;
        return string.Concat(Enumerable.Range(0, 3).Select(_ => letters[random.Next(letters.Length)]))
               + "010203"
               + string.Concat(Enumerable.Range(0, 3).Select(_ => alnum[random.Next(alnum.Length)]));
    }

    public static CreateSupplierRequest NewSupplierRequest(string? taxId = null) =>
        new(taxId ?? NewTaxId(), CatalogTestHelpers.Unique("Proveedor"), "Marta Ríos", "33 1234 5678", "ventas@proveedor.mx", 30);

    public static async Task<SupplierDto> CreateSupplierAsync(this HttpClient client, CreateSupplierRequest? request = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/suppliers", request ?? NewSupplierRequest());
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.ReadJsonAsync<SupplierDto>())!;
    }

    public static Task<HttpResponseMessage> AddSupplierItemAsync(
        this HttpClient client, Guid supplierId, Guid itemId, decimal price = 100m, bool preferred = false, string? supplierSku = null) =>
        client.PostAsJsonAsync($"/api/v1/suppliers/{supplierId}/items",
            new CreateSupplierItemRequest(itemId, supplierSku, price, 3, preferred));
}

[Collection(ApiCollection.Name)]
public class SupplierTests(SgoApiFactory factory)
{
    private Task<HttpClient> AdminAsync() =>
        factory.CreateAuthenticatedClientAsync(SgoApiFactory.AdminEmail, SgoApiFactory.AdminPassword);

    private static async Task<T> OkAsync<T>(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.ReadJsonAsync<T>())!;
    }

    private static Task<HttpResponseMessage> UpdateItemAsync(HttpClient client, SupplierItemDto row, bool preferred, bool active = true, decimal? price = null) =>
        client.PutAsJsonAsync($"/api/v1/suppliers/{row.SupplierId}/items/{row.Id}",
            new UpdateSupplierItemRequest(row.Version, row.SupplierSku, price ?? row.Price, row.LeadTimeDays, preferred, active));

    private static async Task<SupplierItemDto> RowAsync(HttpClient client, SupplierItemDto row) =>
        (await client.GetJsonAsync<SupplierItemDto>($"/api/v1/suppliers/{row.SupplierId}/items/{row.Id}"))!;

    private static UpdateSupplierRequest UpdateOf(SupplierDto s, bool isActive = true) =>
        new(s.Version, s.TaxId, s.Name, s.ContactName, s.Phone, s.Email, s.PaymentTermsDays, isActive);

    [Fact]
    public async Task Supplier_is_created_edited_searched_and_deactivated()
    {
        var admin = await AdminAsync();
        var taxId = PurchasingTestHelpers.NewTaxId();
        var created = await admin.CreateSupplierAsync(PurchasingTestHelpers.NewSupplierRequest(taxId.ToLowerInvariant()));
        Assert.Equal(taxId, created.TaxId);
        Assert.True(created.IsActive);

        var updated = await OkAsync<SupplierDto>(await admin.PutAsJsonAsync($"/api/v1/suppliers/{created.Id}",
            UpdateOf(created) with { Name = "Harinas del Pacífico " + taxId, PaymentTermsDays = 45 }));
        Assert.Equal(45, updated.PaymentTermsDays);

        var byTaxId = (await admin.GetJsonAsync<PagedResult<SupplierDto>>($"/api/v1/suppliers?q={taxId.ToLowerInvariant()}"))!;
        Assert.Equal(created.Id, Assert.Single(byTaxId.Items).Id);

        // Stale version.
        var stale = await admin.PutAsJsonAsync($"/api/v1/suppliers/{created.Id}", UpdateOf(created));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        await OkAsync<SupplierDto>(await admin.PutAsJsonAsync($"/api/v1/suppliers/{created.Id}", UpdateOf(updated, isActive: false)));
        Assert.Empty((await admin.GetJsonAsync<PagedResult<SupplierDto>>($"/api/v1/suppliers?q={taxId}"))!.Items);
        Assert.Single((await admin.GetJsonAsync<PagedResult<SupplierDto>>($"/api/v1/suppliers?q={taxId}&includeInactive=true"))!.Items);
    }

    [Fact]
    public async Task Tax_id_is_unique_except_for_generic_ones()
    {
        var admin = await AdminAsync();
        var existing = await admin.CreateSupplierAsync();

        var duplicate = await admin.PostAsJsonAsync("/api/v1/suppliers", PurchasingTestHelpers.NewSupplierRequest(existing.TaxId.ToLowerInvariant()));
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.True((await duplicate.ProblemAsync()).GetProperty("errors").TryGetProperty("taxId", out _));

        var other = await admin.CreateSupplierAsync();
        var renamedToExisting = await admin.PutAsJsonAsync($"/api/v1/suppliers/{other.Id}", UpdateOf(other) with { TaxId = existing.TaxId });
        Assert.Equal(HttpStatusCode.BadRequest, renamedToExisting.StatusCode);

        var invalid = await admin.PostAsJsonAsync("/api/v1/suppliers", PurchasingTestHelpers.NewSupplierRequest("ABC-123"));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        // Foreign suppliers share the SAT generic RFC.
        await admin.CreateSupplierAsync(PurchasingTestHelpers.NewSupplierRequest("XEXX010101000"));
        await admin.CreateSupplierAsync(PurchasingTestHelpers.NewSupplierRequest("XEXX010101000"));
    }

    [Fact]
    public async Task Supplier_items_show_purchase_unit_and_reject_duplicates_and_inactive_items()
    {
        var admin = await AdminAsync();
        var supplier = await admin.CreateSupplierAsync();
        var flour = await admin.CreateItemAsync(await admin.NewItemRequestAsync()); // purchased by "caja" of 25 kg

        var row = await OkAsync<SupplierItemDto>(await admin.AddSupplierItemAsync(supplier.Id, flour.Id, 412.5m, supplierSku: "HP-TRIGO-25"));
        Assert.Equal((flour.Sku, "caja", 25m, 412.5m, "HP-TRIGO-25", true, false),
            (row.Sku, row.PurchaseUomCode, row.PurchaseToBaseFactor, row.Price, row.SupplierSku, row.IsActive, row.IsPreferred));

        var duplicate = await admin.AddSupplierItemAsync(supplier.Id, flour.Id);
        Assert.Equal("supplier_item_duplicate", await duplicate.ProblemCodeAsync());

        var inactiveRequest = await admin.NewItemRequestAsync();
        var inactive = await admin.CreateItemAsync(inactiveRequest);
        await OkAsync<ItemDto>(await admin.PutAsJsonAsync($"/api/v1/items/{inactive.Id}", new UpdateItemRequest(inactive.Version,
            inactive.Sku, inactive.Name, inactive.Type, inactive.CategoryId, inactive.BaseUomId, inactive.PurchaseUomId,
            inactive.PurchaseToBaseFactor, inactive.TracksLots, inactive.ShelfLifeDays, inactive.StorageCondition, inactive.TaxRate, false)));
        Assert.Equal("item_inactive", await (await admin.AddSupplierItemAsync(supplier.Id, inactive.Id)).ProblemCodeAsync());

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.AddSupplierItemAsync(supplier.Id, Guid.NewGuid())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.AddSupplierItemAsync(Guid.NewGuid(), flour.Id)).StatusCode);

        // Price edit, then deactivation hides it unless includeInactive.
        row = await OkAsync<SupplierItemDto>(await UpdateItemAsync(admin, row, preferred: false, price: 425m));
        Assert.Equal(425m, row.Price);
        var search = (await admin.GetJsonAsync<PagedResult<SupplierItemDto>>($"/api/v1/suppliers/{supplier.Id}/items?q=hp-trigo"))!;
        Assert.Equal(row.Id, Assert.Single(search.Items).Id);

        await OkAsync<SupplierItemDto>(await UpdateItemAsync(admin, row, preferred: false, active: false));
        Assert.Empty((await admin.GetJsonAsync<PagedResult<SupplierItemDto>>($"/api/v1/suppliers/{supplier.Id}/items"))!.Items);
        Assert.Single((await admin.GetJsonAsync<PagedResult<SupplierItemDto>>($"/api/v1/suppliers/{supplier.Id}/items?includeInactive=true"))!.Items);

        var stale = await UpdateItemAsync(admin, row, preferred: false);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
    }

    [Fact]
    public async Task Only_one_preferred_supplier_per_item()
    {
        var admin = await AdminAsync();
        var item = await admin.CreateItemAsync(await admin.NewItemRequestAsync());
        var first = await admin.CreateSupplierAsync();
        var second = await admin.CreateSupplierAsync();

        var a = await OkAsync<SupplierItemDto>(await admin.AddSupplierItemAsync(first.Id, item.Id, 100m, preferred: true));
        Assert.True(a.IsPreferred);

        // Adding the item to another supplier as preferred moves the preference.
        var b = await OkAsync<SupplierItemDto>(await admin.AddSupplierItemAsync(second.Id, item.Id, 95m, preferred: true));
        Assert.True(b.IsPreferred);
        a = await RowAsync(admin, a);
        Assert.False(a.IsPreferred);

        // Editing moves it back.
        a = await OkAsync<SupplierItemDto>(await UpdateItemAsync(admin, a, preferred: true));
        Assert.True(a.IsPreferred);
        Assert.False((await RowAsync(admin, b)).IsPreferred);

        // Deactivating the row removes the preference.
        a = await OkAsync<SupplierItemDto>(await UpdateItemAsync(admin, a, preferred: false, active: false));
        Assert.False(a.IsPreferred);
        var inactivePreferred = await UpdateItemAsync(admin, a, preferred: true, active: false);
        Assert.Equal(HttpStatusCode.BadRequest, inactivePreferred.StatusCode);
    }

    [Fact]
    public async Task Deactivating_a_supplier_unmarks_its_preferred_items_and_locks_its_items()
    {
        var admin = await AdminAsync();
        var item = await admin.CreateItemAsync(await admin.NewItemRequestAsync());
        var supplier = await admin.CreateSupplierAsync();
        var row = await OkAsync<SupplierItemDto>(await admin.AddSupplierItemAsync(supplier.Id, item.Id, preferred: true));

        var inactive = await OkAsync<SupplierDto>(await admin.PutAsJsonAsync($"/api/v1/suppliers/{supplier.Id}", UpdateOf(supplier, isActive: false)));

        row = await RowAsync(admin, row);
        Assert.False(row.IsPreferred);
        Assert.True(row.IsActive);
        Assert.Equal("supplier_inactive", await (await UpdateItemAsync(admin, row, preferred: true)).ProblemCodeAsync());
        var other = await admin.CreateItemAsync(await admin.NewItemRequestAsync());
        Assert.Equal("supplier_inactive", await (await admin.AddSupplierItemAsync(supplier.Id, other.Id)).ProblemCodeAsync());

        // Reactivated, it can be marked preferred again.
        await OkAsync<SupplierDto>(await admin.PutAsJsonAsync($"/api/v1/suppliers/{supplier.Id}", UpdateOf(inactive)));
        Assert.True((await OkAsync<SupplierItemDto>(await UpdateItemAsync(admin, row, preferred: true))).IsPreferred);
    }

    [Fact]
    public async Task Supplier_items_of_another_supplier_are_not_found()
    {
        var admin = await AdminAsync();
        var item = await admin.CreateItemAsync(await admin.NewItemRequestAsync());
        var owner = await admin.CreateSupplierAsync();
        var other = await admin.CreateSupplierAsync();
        var row = await OkAsync<SupplierItemDto>(await admin.AddSupplierItemAsync(owner.Id, item.Id));

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/suppliers/{other.Id}/items/{row.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync($"/api/v1/suppliers/{other.Id}/items/{row.Id}",
            new UpdateSupplierItemRequest(row.Version, null, 1, 0, false, true))).StatusCode);
    }

    [Fact]
    public async Task Supplier_permissions()
    {
        var admin = await AdminAsync();
        var supplier = await admin.CreateSupplierAsync();
        var item = await admin.CreateItemAsync(await admin.NewItemRequestAsync());

        var viewer = await factory.CreateUserAsync("Consulta", "FAB");
        var viewerClient = await factory.CreateAuthenticatedClientAsync(viewer.Email, viewer.Password);
        Assert.Equal(HttpStatusCode.OK, (await viewerClient.GetAsync("/api/v1/suppliers")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewerClient.GetAsync($"/api/v1/suppliers/{supplier.Id}/items")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewerClient.PostAsJsonAsync("/api/v1/suppliers", PurchasingTestHelpers.NewSupplierRequest())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewerClient.AddSupplierItemAsync(supplier.Id, item.Id)).StatusCode);

        var buyer = await factory.CreateUserAsync("Compras", "FAB");
        var buyerClient = await factory.CreateAuthenticatedClientAsync(buyer.Email, buyer.Password);
        var created = await buyerClient.CreateSupplierAsync();
        Assert.Equal(HttpStatusCode.Created, (await buyerClient.AddSupplierItemAsync(created.Id, item.Id)).StatusCode);
    }
}
