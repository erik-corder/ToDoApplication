using System;
using System.Linq;
using Application.Tasks.Dtos;

namespace Application.Tasks;

public static class TaskMappingExtensions
{
    public static TaskDto ToDto(this Domain.Entities.Task t)
    {
        return new TaskDto
        {
            Id = t.Id,
            OrganizationId = t.OrganizationId,
            BoardId = t.BoardId,
            ColumnId = t.ColumnId,
            Title = t.Title,
            Description = t.Description,
            Status = t.Status,
            Priority = t.Priority,
            AssigneeUserId = t.AssigneeUserId,
            ReporterUserId = t.ReporterUserId,
            DueDate = t.DueDate,
            OrderIndex = t.OrderIndex,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            RowVersion = Convert.ToBase64String(t.RowVersion ?? Array.Empty<byte>())
        };
    }
}
