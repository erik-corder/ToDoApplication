using System;

namespace Application.Tasks.Dtos;

public class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? AssigneeUserId { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public string Priority { get; set; } = "Medium";
}
