using static Sgo.Domain.Security.Permissions;

namespace Sgo.Domain.Security;

/// <summary>Predefined roles seeded on first run (dominio §6). They remain editable afterwards.</summary>
public static class SystemRoles
{
    public const string Administrator = "Administrador";

    public sealed record Definition(string Name, string Description, IReadOnlyList<string> Permissions);

    private static IEnumerable<string> Matching(Func<string, bool> predicate) => Codes.Where(predicate);
    // "Todos los *.view" = module views. The audit log is not included: the spec grants
    // security.audit.view explicitly (and separately) only to the operations manager.
    private static IEnumerable<string> Views =>
        Matching(c => c.EndsWith(".view", StringComparison.Ordinal) && c != SecurityAuditView);
    private static IEnumerable<string> Module(string prefix) => Matching(c => c.StartsWith(prefix + ".", StringComparison.Ordinal));

    public static readonly IReadOnlyList<Definition> All =
    [
        new(Administrator, "Acceso total al sistema", [.. Codes]),
        new("Gerente de operaciones", "Consulta general, aprobaciones y bitácora",
            [.. Views, LogisticsOrdersApprove, PurchasingPoApprove, LocationsAll, SecurityAuditView]),
        new("Compras", "Proveedores, requisiciones, órdenes de compra y recepciones",
            [.. Module("purchasing").Where(c => c != PurchasingPoApprove), CatalogView, InventoryView]),
        new("Jefe de producción", "Recetas y órdenes de producción",
            [.. Module("production"), InventoryView, InventoryCount, CatalogView]),
        new("Almacén comisariato/fábrica", "Inventario, despacho de traspasos y recepción de compras",
            [.. Module("inventory"), LogisticsView, LogisticsOrdersApprove, LogisticsTransfersDispatch, PurchasingReceive, PurchasingView]),
        new("Encargado de sucursal", "Operación diaria de sucursal",
            [InventoryView, InventoryCount, InventoryConsumption, LogisticsView, LogisticsOrdersCreate, LogisticsTransfersReceive]),
        new("Consulta", "Solo lectura", [.. Views]),
    ];
}
