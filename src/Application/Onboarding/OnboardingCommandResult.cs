using System.Collections.Generic;

namespace Kanban.Application.Onboarding;

public sealed class OnboardingCommandResult
{
    public CreateOrganizationResponse? Value { get; private set; }
    public Dictionary<string, string> Errors { get; private set; } = new();
    public bool IsValidationFailure { get; private set; }
    public bool IsSlugConflict { get; private set; }

    public static OnboardingCommandResult Success(CreateOrganizationResponse value)
        => new() { Value = value };

    public static OnboardingCommandResult Validation(Dictionary<string, string> errors)
        => new() { Errors = errors, IsValidationFailure = true };

    public static OnboardingCommandResult Conflict(Dictionary<string, string> errors)
        => new() { Errors = errors, IsSlugConflict = true };
}
