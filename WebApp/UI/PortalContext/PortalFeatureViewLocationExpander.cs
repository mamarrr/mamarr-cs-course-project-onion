using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;

namespace WebApp.UI.PortalContext;

public class PortalFeatureViewLocationExpander : IViewLocationExpander
{
    private const string FeatureKey = "portalFeature";

    public void PopulateValues(ViewLocationExpanderContext context)
    {
        if (!string.Equals(context.AreaName, "Portal", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (context.ActionContext.ActionDescriptor is not ControllerActionDescriptor descriptor)
        {
            return;
        }

        var controllerNamespace = descriptor.ControllerTypeInfo.Namespace;
        const string marker = ".Controllers.";
        var markerIndex = controllerNamespace?.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex is null or < 0)
        {
            return;
        }

        var feature = controllerNamespace![(markerIndex.Value + marker.Length)..].Split('.').FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(feature))
        {
            context.Values[FeatureKey] = feature;
        }
    }

    public IEnumerable<string> ExpandViewLocations(
        ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations)
    {
        if (context.Values.TryGetValue(FeatureKey, out var feature))
        {
            yield return $"/Areas/{{2}}/Views/{feature}/{{1}}/{{0}}.cshtml";
            yield return $"/Areas/{{2}}/Views/{feature}/Shared/{{0}}.cshtml";
        }

        foreach (var location in viewLocations)
        {
            yield return location;
        }
    }
}
