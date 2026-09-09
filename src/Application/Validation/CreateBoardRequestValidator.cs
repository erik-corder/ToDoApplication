using System.Collections.Generic;
using Kanban.Application.Boards;

namespace Kanban.Application.Validation;

public sealed class CreateBoardRequestValidator
{
    public Dictionary<string, string> Validate(CreateBoardRequest request)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
            errors["name"] = "Name is required and must be 100 characters or fewer.";
        return errors;
    }
}
