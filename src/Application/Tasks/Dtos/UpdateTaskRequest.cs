using System;

namespace Application.Tasks.Dtos;

public class UpdateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? AssigneeUserId { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Open";
    public string? RowVersion { get; set; } // optional alt to If-Match
}
