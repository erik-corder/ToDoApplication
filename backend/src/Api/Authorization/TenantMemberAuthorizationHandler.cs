using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Todo.Api.Authorization;

/// <summary>
/// Enforces that the <see cref="IRequestContext.OrganizationId"/> resolved from the BFF-forwarded
/// identity headers matches the organization scope referenced by the current route.
///
/// The handler supports two route shapes:
/// 1. <c>/w/{orgSlug}</c> or any route with a route value named <c>orgSlug</c> — the handler
///    expects a <c>X-Internal-Org-Slug</c> header from the BFF and matches it against the
///    <see cref="IRequestContext.OrganizationSlug"/>.
/// 2. Any route that omits <c>orgSlug</c> — the handler succeeds as long as an
///    <see cref="IRequestContext"/> is present (tenant scope is then enforced at the data layer
///    by EF Core global query filters keyed to <c>IRequestContext.OrganizationId</c>).
/// </summary>
public sealed class TenantMemberAuthorizationHandler
    : AuthorizationHandler<TenantMemberRequirement>
{
    public const string OrgSlugRouteKey = "orgSlug";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantMemberAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantMemberRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return Task.CompletedTask;
        }

        var requestContext = httpContext.GetRequestContext();
        if (requestContext is null || !requestContext.IsAuthenticated)
        {
            // No principal resolved by RequestContextMiddleware — let the auth pipeline
            // produce a 401 via [Authorize] without the TenantMember policy granting access.
            return Task.CompletedTask;
        }

        var routeData = httpContext.GetRouteData();
        if (routeData is null || !routeData.Values.TryGetValue(OrgSlugRouteKey, out var slugObj))
        {
            // No org-scoped route value; the data layer is responsible for filtering.
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var routeSlug = slugObj?.ToString();
        if (string.IsNullOrEmpty(routeSlug))
        {
            return Task.CompletedTask;
        }

        if (string.Equals(routeSlug, requestContext.OrganizationSlug, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

internal static class HttpContextRequestContextExtensions
{
    public static IRequestContext? GetRequestContext(this HttpContext httpContext)
    {
        return httpContext.Items["RequestContext"] as IRequestContext;
    }

    public static RouteData? GetRouteData(this HttpContext httpContext)
    {
        return httpContext.GetRouteValue("__RouteData") as RouteData;
    }
}
