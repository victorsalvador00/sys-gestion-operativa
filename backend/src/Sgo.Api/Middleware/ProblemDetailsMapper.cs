using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sgo.Domain.Common;

namespace Sgo.Api.Middleware;

/// <summary>Maps exceptions to RFC 9457 ProblemDetails following spec table 6.1.</summary>
public static class ProblemDetailsMapper
{
    public const string TypeBase = "https://sgo/errors/";

    public static ProblemDetails Map(Exception exception) => exception switch
    {
        BusinessRuleException e => Create(StatusCodes.Status422UnprocessableEntity, e.Code, "Regla de negocio incumplida", e.Message),
        InsufficientStockException e => WithShortages(
            Create(StatusCodes.Status409Conflict, "insufficient_stock", "Existencia insuficiente", e.Message), e.Shortages),
        ConcurrencyException e => Create(StatusCodes.Status409Conflict, "concurrency", "Conflicto de concurrencia", e.Message),
        DbUpdateConcurrencyException => Create(StatusCodes.Status409Conflict, "concurrency", "Conflicto de concurrencia",
            new ConcurrencyException().Message),
        NotFoundException e => Create(StatusCodes.Status404NotFound, "not_found", "No encontrado", e.Message),
        ForbiddenException e => Create(StatusCodes.Status403Forbidden, "forbidden", "Acceso denegado", e.Message),
        _ => Create(StatusCodes.Status500InternalServerError, "internal", "Error interno",
            "Ocurrió un error inesperado. Si persiste, contacta al administrador."),
    };

    /// <summary>
    /// Gives framework-generated problems (routing 404, 401/403 from auth, model binding 400)
    /// the SGO type, code and a Spanish title. Problems created by <see cref="Map"/> are left as is.
    /// </summary>
    public static void Normalize(ProblemDetails problem)
    {
        if (problem.Type?.StartsWith(TypeBase, StringComparison.Ordinal) == true)
            return;

        (string Code, string Title)? known = problem.Status switch
        {
            StatusCodes.Status400BadRequest => ("validation", "Solicitud inválida"),
            StatusCodes.Status401Unauthorized => ("unauthorized", "No autenticado"),
            StatusCodes.Status403Forbidden => ("forbidden", "Acceso denegado"),
            StatusCodes.Status404NotFound => ("not_found", "No encontrado"),
            StatusCodes.Status405MethodNotAllowed => ("method_not_allowed", "Método no permitido"),
            StatusCodes.Status429TooManyRequests => ("too_many_requests", "Demasiadas solicitudes. Intenta más tarde."),
            _ => null,
        };
        if (known is null)
            return;

        problem.Type = TypeBase + known.Value.Code;
        problem.Title = known.Value.Title;
        problem.Extensions["code"] = known.Value.Code;
    }

    private static ProblemDetails Create(int status, string code, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Type = TypeBase + code,
            Title = title,
            Detail = detail,
        };
        problem.Extensions["code"] = code;
        return problem;
    }

    private static ProblemDetails WithShortages(ProblemDetails problem, IReadOnlyList<StockShortage> shortages)
    {
        problem.Extensions["shortages"] = shortages;
        return problem;
    }
}
