using System.Collections.Generic;

namespace Kanban.Application.Validation;

public sealed record ValidationProblemResponse(Dictionary<string, string> Errors)
{
    public ValidationProblemResponse() : this(new Dictionary<string, string>()) { }
}
