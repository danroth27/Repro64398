// Partial class file #1: contains the MapPost call.
// Place the caret on any route parameter token (e.g. organizationId, yId, zId, wId)
// inside the route template to trigger "Document Highlights" (Ctrl+K, H / hover).

public static partial class XEndpoints
{
    public static void MapXEndpoints(this IEndpointRouteBuilder app)
    {
        // Move caret to {organizationId} / {yId} / {zId} / {wId} to repro.
        app.MapPost("/organizations/{organizationId}/y/{yId}/z/{zId}/w/{wId}/load", LoadAsync);
    }
}
