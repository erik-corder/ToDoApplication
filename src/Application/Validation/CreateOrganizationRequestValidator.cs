using System.Collections.Generic;
using System.Text.RegularExpressions;
using Kanban.Application.Onboarding;

namespace Kanban.Application.Validation;

public static partial class SlugRegexSource
{
    [System.Text.RegularExpressions.GeneratedRegex(@"^[a-z0-9](?:[a-z0-9-]{0,48}[a-z0-9])?$")]
    public static partial Regex SlugPattern();
}

public sealed class CreateOrganizationRequestValidator
{
    private static readonly Regex SlugRegex = SlugRegexSource.SlugPattern();

    public Dictionary<string, string> Validate(CreateOrganizationRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
            errors["name"] = "Name is required and must be 100 characters or fewer.";

        if (string.IsNullOrEmpty(request.Slug) || request.Slug.Length > 50 || !SlugRegex.IsMatch(request.Slug))
            errors["slug"] = "Slug must be 1-50 chars, lowercase alphanumeric, and may contain single hyphens.";

        return errors;
    }
}
