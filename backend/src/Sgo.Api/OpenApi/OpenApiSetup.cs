using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Sgo.Application.Common;
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
