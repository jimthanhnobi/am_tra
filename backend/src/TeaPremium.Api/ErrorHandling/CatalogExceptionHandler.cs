using Microsoft.AspNetCore.Diagnostics;
using TeaPremium.Application.Catalog;
using TeaPremium.Application.Content;
using TeaPremium.Application.Leads;
using TeaPremium.Application.Media;
using TeaPremium.Application.Settings;

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
            ContentValidationException validation => Results.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Content validation failed.",
                detail: validation.Message),
            ContentNotFoundException notFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Content not found.",
                detail: notFound.Message),
            ContentConflictException conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Content conflict.",
                detail: conflict.Message),
            LeadValidationException validation => Results.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Lead validation failed.",
                detail: validation.Message),
            LeadNotFoundException notFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Lead not found.",
                detail: notFound.Message),
            MediaStorageUnavailableException unavailable => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Media storage unavailable.",
                detail: unavailable.Message),
            SiteSettingsValidationException validation => Results.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Site settings validation failed.",
                detail: validation.Message),
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
