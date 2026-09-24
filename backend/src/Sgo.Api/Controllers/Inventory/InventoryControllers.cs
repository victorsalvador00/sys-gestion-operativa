using Microsoft.AspNetCore.Mvc;
using Sgo.Api.Auth;
using Sgo.Application.Common;
using Sgo.Application.Inventory;
using Sgo.Domain.Common;
using Sgo.Domain.Security;

namespace Sgo.Api.Controllers.Inventory;

[ApiController]
[Tags("Inventario")]
[RequirePermission(Permissions.InventoryView)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class StockController(IStockQueries stock) : ControllerBase
{
    /// <summary>Existencias por ubicación y artículo. belowMin=true muestra solo lo que está bajo el mínimo (RN-07).</summary>
    [HttpGet("stock")]
    public Task<PagedResult<StockLevelDto>> List([FromQuery] StockQuery query, CancellationToken ct) =>
        stock.ListAsync(query, ct);

    /// <summary>Detalle por lote de un artículo en una ubicación, del que caduca primero al último.</summary>
    [HttpGet("stock/{locationId:guid}/{itemId:guid}/lots")]
    public Task<IReadOnlyList<LotStockDto>> Lots(Guid locationId, Guid itemId, CancellationToken ct) =>
        stock.LotsAsync(locationId, itemId, ct);

    /// <summary>Kardex paginado, del más reciente al más antiguo. Con locationId e itemId incluye el saldo acumulado.</summary>
    [HttpGet("movements")]
    public Task<PagedResult<KardexEntryDto>> Movements([FromQuery] MovementQuery query, CancellationToken ct) =>
        stock.MovementsAsync(query, ct);

    /// <summary>Stock bajo y lotes por caducar (o ya vencidos) en tus ubicaciones.</summary>
    [HttpGet("alerts")]
    public Task<AlertsDto> Alerts([FromQuery] Guid? locationId, CancellationToken ct) =>
        stock.AlertsAsync(locationId, ct);
}

[ApiController]
[Route("adjustments")]
[Tags("Inventario")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class AdjustmentsController(IAdjustmentService adjustments) : ControllerBase
{
    [HttpGet]
    [RequirePermission(Permissions.InventoryView)]
    public Task<PagedResult<AdjustmentListItemDto>> List([FromQuery] AdjustmentListQuery query, CancellationToken ct) =>
        adjustments.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.InventoryView)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<AdjustmentDto> Get(Guid id, CancellationToken ct) => adjustments.GetAsync(id, ct);

    /// <summary>
    /// Crea el ajuste y lo registra de inmediato. Corrección admite entradas y salidas; merma, caducado,
    /// dañado y uso interno solo salidas. Sin existencia suficiente responde 409 insufficient_stock y no registra nada.
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.InventoryAdjust)]
    [ProducesResponseType<AdjustmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AdjustmentDto>> Create(CreateAdjustmentRequest request, CancellationToken ct)
    {
        var adjustment = await adjustments.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = adjustment.Id }, adjustment);
    }
}

[ApiController]
[Route("imports")]
[Tags("Importaciones")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class InventoryImportsController(IInitialStockImportService initialStock) : ControllerBase
{
    public const long MaxFileBytes = 2 * 1024 * 1024;

    /// <summary>
    /// Carga existencias iniciales desde CSV UTF-8: ubicacion, sku, cantidad, costo_unitario (obligatorias); lote,
    /// caducidad (artículos con lotes). Genera un ajuste por ubicación. Con errores responde 400 con rowErrors y no importa nada.
    /// </summary>
    [HttpPost("initial-stock")]
    [RequirePermission(Permissions.InventoryAdjust)]
    [RequestSizeLimit(MaxFileBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileBytes + 64 * 1024)]
    [ProducesResponseType<InitialStockImportResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<InitialStockImportResult> ImportInitialStock(IFormFile file, CancellationToken ct)
    {
        if (file.Length > MaxFileBytes)
            throw new ImportValidationException([new(1, null, "El archivo excede el máximo de 2 MB.")]);

        await using var stream = file.OpenReadStream();
        return await initialStock.ImportAsync(stream, ct);
    }
}
