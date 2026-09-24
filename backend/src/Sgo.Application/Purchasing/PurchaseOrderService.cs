using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sgo.Application.Common;
using Sgo.Domain.Common;
using Sgo.Domain.Purchasing;

namespace Sgo.Application.Purchasing;

public sealed record PurchaseOrderListQuery : PageQuery
{
    public PurchaseOrderStatus? Status { get; init; }
    public Guid? SupplierId { get; init; }
    public Guid? LocationId { get; init; }
}

public sealed record PurchasingLocationDto(Guid Id, string Code, string Name);

public sealed record PurchaseOrderSupplierDto(Guid Id, string TaxId, string Name);

public sealed record PurchaseOrderListItemDto(
    Guid Id, string Folio, PurchaseOrderSupplierDto Supplier, PurchasingLocationDto DeliveryLocation, DateOnly? ExpectedDate,
    PurchaseOrderStatus Status, decimal Total, int LineCount, DateTimeOffset CreatedAt);

/// <param name="PurchaseUomCode">Unit of Quantity and UnitPrice: the item's purchase unit, or its base unit.</param>
public sealed record PurchaseOrderLineDto(
    Guid Id, Guid ItemId, string Sku, string ItemName, string PurchaseUomCode, decimal PurchaseToBaseFactor, decimal Quantity,
    decimal UnitPrice, decimal TaxRate, decimal Subtotal, decimal TaxAmount, decimal ReceivedQty,
    Guid? RequisitionLineId, Guid? RequisitionId, string? RequisitionFolio);

public sealed record PurchaseOrderDto(
    Guid Id, string Folio, PurchaseOrderSupplierDto Supplier, PurchasingLocationDto DeliveryLocation, DateOnly? ExpectedDate,
    PurchaseOrderStatus Status, string? Notes, decimal Subtotal, decimal TaxTotal, decimal Total, Guid? ApprovedBy,
    DateTimeOffset? ApprovedAt, IReadOnlyList<PurchaseOrderLineDto> Lines, DateTimeOffset CreatedAt, Guid? CreatedBy, uint Version);

public interface IPurchaseOrderService
{
    Task<PagedResult<PurchaseOrderListItemDto>> ListAsync(PurchaseOrderListQuery query, CancellationToken ct = default);
    Task<PurchaseOrderDto> GetAsync(Guid id, CancellationToken ct = default);
}

/// <summary>Read side of purchase orders. B-13 creates them from requisitions; B-14 adds editing, approval and receipts.</summary>
public sealed class PurchaseOrderService(ISgoDbContext db, ILocationScope scope) : IPurchaseOrderService
{
    private static readonly Dictionary<string, Expression<Func<PurchaseOrder, object?>>> SortColumns = new()
    {
        ["folio"] = o => o.Folio,
        ["createdAt"] = o => o.CreatedAt,
        ["expectedDate"] = o => o.ExpectedDate,
        ["total"] = o => o.Total,
        ["status"] = o => o.Status,
    };

    public async Task<PagedResult<PurchaseOrderListItemDto>> ListAsync(PurchaseOrderListQuery query, CancellationToken ct = default)
    {
        var allowed = scope.AllowedLocationIds;
        var orders = db.PurchaseOrders.AsNoTracking().Where(o => allowed.Contains(o.DeliveryLocationId));
        if (query.Status is { } status)
            orders = orders.Where(o => o.Status == status);
        if (query.SupplierId is { } supplierId)
            orders = orders.Where(o => o.SupplierId == supplierId);
        if (query.LocationId is { } locationId)
            orders = orders.Where(o => o.DeliveryLocationId == locationId);
        if (query.SearchTerm() is { } term)
            orders = orders.Where(o => o.Folio.ToLower().Contains(term)
                                       || db.Suppliers.Any(s => s.Id == o.SupplierId && s.Name.ToLower().Contains(term)));

        var page = await orders.ApplySort(query.Sort, SortColumns, "createdAt:desc").ToPagedResultAsync(query, ct);
        return new PagedResult<PurchaseOrderListItemDto>(await ToListItemsAsync(db, page.Items, ct), page.Page, page.PageSize, page.Total);
    }

    public async Task<PurchaseOrderDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var order = await db.PurchaseOrders.AsNoTracking().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, ct)
                    ?? throw new NotFoundException("la orden de compra", id);
        scope.EnsureAccess(order.DeliveryLocationId);
        return await ToDtoAsync(order, ct);
    }

    internal static async Task<List<PurchaseOrderListItemDto>> ToListItemsAsync(ISgoDbContext db, IReadOnlyList<PurchaseOrder> orders, CancellationToken ct)
    {
        var ids = orders.Select(o => o.Id).ToList();
        var lineCounts = await db.PurchaseOrders.Where(o => ids.Contains(o.Id)).Select(o => new { o.Id, Count = o.Lines.Count })
            .ToDictionaryAsync(x => x.Id, x => x.Count, ct);
        var suppliers = await SuppliersAsync(db, orders.Select(o => o.SupplierId), ct);
        var locations = await LocationsAsync(db, orders.Select(o => o.DeliveryLocationId), ct);
        return orders.Select(o => new PurchaseOrderListItemDto(o.Id, o.Folio, suppliers[o.SupplierId], locations[o.DeliveryLocationId],
            o.ExpectedDate, o.Status, o.Total, lineCounts[o.Id], o.CreatedAt)).ToList();
    }

    private async Task<PurchaseOrderDto> ToDtoAsync(PurchaseOrder o, CancellationToken ct)
    {
        var suppliers = await SuppliersAsync(db, [o.SupplierId], ct);
        var locations = await LocationsAsync(db, [o.DeliveryLocationId], ct);
        var itemIds = o.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await (from i in db.Items.AsNoTracking()
                           join u in db.UnitsOfMeasure on i.PurchaseUomId ?? i.BaseUomId equals u.Id
                           where itemIds.Contains(i.Id)
                           select new { i.Id, i.Sku, i.Name, Uom = u.Code, i.PurchaseToBaseFactor }).ToDictionaryAsync(i => i.Id, ct);
        var requisitionLineIds = o.Lines.Select(l => l.RequisitionLineId).OfType<Guid>().ToList();
        var requisitions = await db.PurchaseRequisitions.AsNoTracking()
            .SelectMany(r => r.Lines.Where(l => requisitionLineIds.Contains(l.Id)), (r, l) => new { LineId = l.Id, r.Id, r.Folio })
            .ToDictionaryAsync(x => x.LineId, ct);

        var lines = o.Lines.Select(l =>
        {
            var item = items[l.ItemId];
            var requisition = l.RequisitionLineId is { } rl ? requisitions[rl] : null;
            return new PurchaseOrderLineDto(l.Id, l.ItemId, item.Sku, item.Name, item.Uom, item.PurchaseToBaseFactor, l.Quantity,
                l.UnitPrice, l.TaxRate, l.Subtotal, l.TaxAmount, l.ReceivedQty, l.RequisitionLineId, requisition?.Id, requisition?.Folio);
        }).OrderBy(l => l.Sku).ToList();

        return new PurchaseOrderDto(o.Id, o.Folio, suppliers[o.SupplierId], locations[o.DeliveryLocationId], o.ExpectedDate, o.Status,
            o.Notes, o.Subtotal, o.TaxTotal, o.Total, o.ApprovedBy, o.ApprovedAt, lines, o.CreatedAt, o.CreatedBy, o.Version);
    }

    internal static async Task<Dictionary<Guid, PurchaseOrderSupplierDto>> SuppliersAsync(ISgoDbContext db, IEnumerable<Guid> ids, CancellationToken ct)
    {
        var list = ids.Distinct().ToList();
        return await db.Suppliers.AsNoTracking().Where(s => list.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => new PurchaseOrderSupplierDto(s.Id, s.TaxId, s.Name), ct);
    }

    internal static async Task<Dictionary<Guid, PurchasingLocationDto>> LocationsAsync(ISgoDbContext db, IEnumerable<Guid> ids, CancellationToken ct)
    {
        var list = ids.Distinct().ToList();
        return await db.Locations.AsNoTracking().Where(l => list.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => new PurchasingLocationDto(l.Id, l.Code, l.Name), ct);
    }
}
