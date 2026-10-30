using Microsoft.AspNetCore.Diagnostics;
using TeaPremium.Application.Catalog;
using TeaPremium.Application.Media;

namespace TeaPremium.Api.ErrorHandling;

internal sealed class CatalogExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        IResult? result = exception switch
        {
            CatalogValidationException validation => Results.ValidationProblem(
                validation.Errors,
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Catalog validation failed.",
                detail: validation.Message),
            CatalogNotFoundException notFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Catalog resource not found.",
                detail: notFound.Message),
            CatalogConflictException conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Catalog conflict.",
                detail: conflict.Message),
            MediaStorageUnavailableException unavailable => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Media storage unavailable.",
                detail: unavailable.Message),
            ObjectStorageException => Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Media storage request failed.",
                detail: "The object storage provider could not complete the request."),
            _ => null,
        };

        if (result is null)
        {
            return false;
        }

        await result.ExecuteAsync(httpContext);
        return true;
    }
}
