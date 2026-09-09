using System.Collections.Generic;

namespace Kanban.Application.Boards;

public sealed class CreateBoardCommandResult
{
    public CreateBoardResponse? Value { get; private set; }
    public Dictionary<string, string> Errors { get; private set; } = new();
    public bool IsValidationFailure { get; private set; }
    public bool IsForbidden { get; private set; }

    public static CreateBoardCommandResult Success(CreateBoardResponse value) => new() { Value = value };
    public static CreateBoardCommandResult Validation(Dictionary<string, string> errors)
        => new() { Errors = errors, IsValidationFailure = true };
    public static CreateBoardCommandResult Forbidden() => new() { IsForbidden = true };
}
