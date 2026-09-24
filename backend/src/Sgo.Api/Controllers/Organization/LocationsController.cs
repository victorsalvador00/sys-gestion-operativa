using Microsoft.AspNetCore.Mvc;
using Sgo.Api.Auth;
using Sgo.Application.Common;
using Sgo.Application.Organization;
using Sgo.Domain.Security;

namespace Sgo.Api.Controllers.Organization;

[ApiController]
[Route("locations")]
[Tags("Ubicaciones")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class LocationsController(ILocationService locations) : ControllerBase
{
    /// <summary>Ubicaciones a las que tienes acceso. Activas por defecto; includeInactive=true para ver todas.</summary>
    [HttpGet]
    [RequirePermission(Permissions.LocationsView)]
    public Task<PagedResult<LocationDto>> List([FromQuery] LocationListQuery query, CancellationToken ct) =>
        locations.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.LocationsView)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<LocationDto> Get(Guid id, CancellationToken ct) => locations.GetAsync(id, ct);

    /// <summary>Edita nombre, dirección y estado. El código y el tipo no cambian.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.LocationsManage)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<LocationDto> Update(Guid id, UpdateLocationRequest request, CancellationToken ct) =>
        locations.UpdateAsync(id, request, ct);
}
