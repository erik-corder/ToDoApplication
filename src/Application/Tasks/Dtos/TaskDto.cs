using System;

namespace Application.Tasks.Dtos;

public class TaskDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BoardId { get; set; }
    public Guid ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Medium";
    public Guid? AssigneeUserId { get; set; }
    public Guid ReporterUserId { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public int OrderIndex { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string RowVersion { get; set; } = string.Empty; // base64
}
