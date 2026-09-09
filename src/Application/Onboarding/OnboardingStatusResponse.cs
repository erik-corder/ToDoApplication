using System;

namespace Kanban.Application.Onboarding;

public sealed record OnboardingStatusResponse(
    bool NeedsOnboarding,
    Guid UserId,
    Guid? ActiveOrganizationId = null,
    Guid? ActiveWorkspaceId = null);
