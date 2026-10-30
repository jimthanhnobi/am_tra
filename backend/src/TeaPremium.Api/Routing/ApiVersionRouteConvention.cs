using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace TeaPremium.Api.Routing;

public sealed class ApiVersionRouteConvention : IApplicationModelConvention
{
    public const string Prefix = "api/v1";

    private readonly AttributeRouteModel _prefix = new(new RouteAttribute(Prefix));

    public void Apply(ApplicationModel application)
    {
        ArgumentNullException.ThrowIfNull(application);

        foreach (var controller in application.Controllers)
        {
            foreach (var selector in controller.Selectors)
            {
                selector.AttributeRouteModel = selector.AttributeRouteModel is null
                    ? _prefix
                    : AttributeRouteModel.CombineAttributeRouteModel(
                        _prefix,
                        selector.AttributeRouteModel);
            }
        }
    }
}

