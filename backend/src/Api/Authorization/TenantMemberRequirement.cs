using Microsoft.AspNetCore.Authorization;

namespace Todo.Api.Authorization;

/// <summary>
/// Requirement that asserts the current request's <see cref="IRequestContext.OrganizationId"/>
/// matches the organization scope implied by the route (e.g., <c>orgSlug</c>).
/// </summary>
public sealed class TenantMemberRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "TenantMember";
}
