using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Sgo.Application.Catalog;
using Sgo.Application.Common;
using Sgo.Application.Inventory;
using Sgo.Application.Logistics;
using Sgo.Application.Organization;
using Sgo.Application.Security;

namespace Sgo.Api.OpenApi;

public static class OpenApiSetup
{
    /// <summary>Request examples shown in the OpenAPI document (definition of done).</summary>
    private static readonly Dictionary<Type, string> RequestExamples = new()
    {
        [typeof(LoginRequest)] = """{ "email": "encargado.suc01@ejemplo.mx", "password": "Contrasena123" }""",
        [typeof(ChangePasswordRequest)] = """{ "currentPassword": "Contrasena123", "newPassword": "NuevaContrasena456" }""",
        [typeof(CreateUserRequest)] = """
            { "email": "encargado.suc01@ejemplo.mx", "fullName": "Laura Méndez", "password": "Temporal2024x",
              "roleIds": ["0199a1b2-0000-7000-8000-000000000001"],
              "locationIds": ["0199a1b2-0000-7000-8000-0000000000a1"], "defaultLocationId": "0199a1b2-0000-7000-8000-0000000000a1" }
            """,
        [typeof(UpdateUserRequest)] = """
            { "version": 1234, "fullName": "Laura Méndez Ruiz",
              "roleIds": ["0199a1b2-0000-7000-8000-000000000001"],
              "locationIds": ["0199a1b2-0000-7000-8000-0000000000a1", "0199a1b2-0000-7000-8000-0000000000a2"],
              "defaultLocationId": "0199a1b2-0000-7000-8000-0000000000a1" }
            """,
        [typeof(ResetPasswordRequest)] = """{ "newPassword": "Temporal2024x" }""",
        [typeof(VersionRequest)] = """{ "version": 1234 }""",
        [typeof(CreateRoleRequest)] = """
            { "name": "Supervisor de sucursales", "description": "Consulta y aprobación de pedidos",
              "permissions": ["inventory.view", "logistics.view", "logistics.orders.approve"] }
            """,
        [typeof(UpdateRoleRequest)] = """
            { "version": 1234, "name": "Supervisor de sucursales", "description": "Consulta y aprobación de pedidos",
              "permissions": ["inventory.view", "logistics.view", "logistics.orders.approve", "locations.view"] }
            """,
        [typeof(CreateLocationRequest)] = """{ "code": "SUC-11", "name": "Sucursal Plaza Sur", "type": "Branch", "address": "Av. Sur 200" }""",
        [typeof(CreateUnitOfMeasureRequest)] = """{ "code": "bolsa", "name": "Bolsa", "kind": "Unit" }""",
        [typeof(UpdateUnitOfMeasureRequest)] = """{ "version": 1234, "name": "Bolsa", "kind": "Unit", "isActive": true }""",
        [typeof(CreateItemCategoryRequest)] = """{ "name": "Lácteos" }""",
        [typeof(UpdateItemCategoryRequest)] = """{ "version": 1234, "name": "Lácteos y derivados", "isActive": true }""",
        [typeof(CreateItemRequest)] = """
            { "sku": "HAR-001", "name": "Harina de trigo", "type": "RawMaterial",
              "categoryId": "0199a1b2-0000-7000-8000-0000000000c1", "baseUomId": "0199a1b2-0000-7000-8000-0000000000b1",
              "purchaseUomId": "0199a1b2-0000-7000-8000-0000000000b2", "purchaseToBaseFactor": 25,
              "tracksLots": true, "shelfLifeDays": 180, "storageCondition": "Ambient", "taxRate": 0 }
            """,
        [typeof(UpdateItemRequest)] = """
            { "version": 1234, "sku": "HAR-001", "name": "Harina de trigo 25 kg", "type": "RawMaterial",
              "categoryId": "0199a1b2-0000-7000-8000-0000000000c1", "baseUomId": "0199a1b2-0000-7000-8000-0000000000b1",
              "purchaseUomId": "0199a1b2-0000-7000-8000-0000000000b2", "purchaseToBaseFactor": 25,
              "tracksLots": true, "shelfLifeDays": 180, "storageCondition": "Ambient", "taxRate": 0, "isActive": true }
            """,
        [typeof(UpdateItemLocationSettingsRequest)] = """
            { "settings": [
                { "locationId": "0199a1b2-0000-7000-8000-0000000000a1", "minQty": 10, "maxQty": 40 },
                { "locationId": "0199a1b2-0000-7000-8000-0000000000a2", "minQty": null, "maxQty": null } ] }
            """,
        [typeof(CreateAdjustmentRequest)] = """
            { "locationId": "0199a1b2-0000-7000-8000-0000000000a1", "reason": "Correction", "notes": "Diferencia en revisión",
              "lines": [
                { "itemId": "0199a1b2-0000-7000-8000-0000000000d1", "lotId": null, "lotNumber": "L-2409", "expirationDate": "2027-03-31",
                  "quantity": 5, "unitCost": 18.5, "notes": null },
                { "itemId": "0199a1b2-0000-7000-8000-0000000000d2", "lotId": null, "lotNumber": null, "expirationDate": null,
                  "quantity": -2, "unitCost": null, "notes": "Bolsa rota" } ] }
            """,
        [typeof(CreatePhysicalCountRequest)] = """{ "locationId": "0199a1b2-0000-7000-8000-0000000000a1", "categoryId": null, "notes": "Conteo semanal" }""",
        [typeof(UpdatePhysicalCountRequest)] = """
            { "version": 1234, "categoryId": null, "notes": "Conteo semanal",
              "counts": [
                { "lineId": "0199a1b2-0000-7000-8000-0000000000e1", "itemId": null, "lotId": null, "lotNumber": null, "expirationDate": null, "countedQty": 7 },
                { "lineId": null, "itemId": "0199a1b2-0000-7000-8000-0000000000d1", "lotId": null, "lotNumber": "L-2410", "expirationDate": "2027-01-15", "countedQty": 2 } ] }
            """,
        [typeof(CreateConsumptionRequest)] = """
            { "locationId": "0199a1b2-0000-7000-8000-0000000000a1", "businessDate": "2026-09-23", "notes": null,
              "lines": [ { "itemId": "0199a1b2-0000-7000-8000-0000000000d1", "lotId": null, "quantity": 1.5 } ] }
            """,
        [typeof(CreateTransferRequest)] = """
            { "fromLocationId": "0199a1b2-0000-7000-8000-0000000000a9", "toLocationId": "0199a1b2-0000-7000-8000-0000000000a1",
              "notes": "Reposición semanal", "lines": [ { "itemId": "0199a1b2-0000-7000-8000-0000000000d1", "lotId": null, "quantity": 12 } ] }
            """,
        [typeof(UpdateTransferRequest)] = """
            { "version": 1234, "toLocationId": "0199a1b2-0000-7000-8000-0000000000a1", "notes": null,
              "lines": [ { "itemId": "0199a1b2-0000-7000-8000-0000000000d1", "lotId": null, "quantity": 15 } ] }
            """,
        [typeof(DispatchTransferRequest)] = """
            { "version": 1234, "vehicleDescription": "Nissan NP300 blanca ABC-123", "driverName": "Juan Pérez",
              "lines": [ { "lineId": "0199a1b2-0000-7000-8000-0000000000e1",
                           "lots": [ { "lotId": "0199a1b2-0000-7000-8000-0000000000f1", "quantity": 10 },
                                     { "lotId": "0199a1b2-0000-7000-8000-0000000000f2", "quantity": 2 } ] } ] }
            """,
        [typeof(ReceiveTransferRequest)] = """
            { "version": 1234, "lines": [
                { "lineId": "0199a1b2-0000-7000-8000-0000000000e1", "receivedQty": 10, "discrepancyReason": null, "discrepancyNotes": null },
                { "lineId": "0199a1b2-0000-7000-8000-0000000000e2", "receivedQty": 1, "discrepancyReason": "Damaged", "discrepancyNotes": "Caja aplastada" } ] }
            """,
        [typeof(UpdateLocationRequest)] = """{ "version": 1234, "name": "Sucursal Centro", "address": "Av. Juárez 100, Col. Centro", "isActive": true }""",
    };

    public static IServiceCollection AddSgoOpenApi(this IServiceCollection services) =>
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "SGO API";
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Access token obtenido en POST /api/v1/auth/login",
                };
                document.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] }];
                return Task.CompletedTask;
            });

            options.AddSchemaTransformer((schema, context, _) =>
            {
                if (RequestExamples.TryGetValue(context.JsonTypeInfo.Type, out var json))
                    schema.Examples = [JsonNode.Parse(json)!];
                return Task.CompletedTask;
            });
        });
}
