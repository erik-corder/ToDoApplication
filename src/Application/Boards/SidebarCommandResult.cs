namespace Kanban.Application.Boards;

public sealed class SidebarCommandResult
{
    public SidebarResponse? Value { get; private set; }
    public bool IsForbidden { get; private set; }

    public static SidebarCommandResult Success(SidebarResponse value) => new() { Value = value };
    public static SidebarCommandResult Forbidden() => new() { IsForbidden = true };
}
