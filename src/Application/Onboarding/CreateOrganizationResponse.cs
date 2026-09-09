using System;

namespace Kanban.Application.Onboarding;

public sealed record CreateOrganizationResponse(
    Guid OrganizationId,
    Guid WorkspaceId,
    Guid MembershipId);
