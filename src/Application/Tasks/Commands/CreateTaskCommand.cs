using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Tasks.Dtos;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Tasks.Commands;

public record CreateTaskCommand(
    Guid BoardId,
    Guid ColumnId,
    Guid OrganizationId,
    Guid ReporterUserId,
    CreateTaskRequest Request) : IRequest<TaskDto>;

public class BoardOrColumnNotFoundInTenantException : Exception { }

public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskDto>
{
    private readonly IAppDbContext _db;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;

    public CreateTaskCommandHandler(IAppDbContext db, IAuditWriter audit, IOutboxWriter outbox)
    {
        _db = db;
        _audit = audit;
        _outbox = outbox;
    }

    public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _db.BeginTransactionAsync(cancellationToken);

        // Tenant-scoped board/column existence (global filter applies)
        var boardExists = await _db.Boards.AnyAsync(b => b.Id == request.BoardId, cancellationToken);
        var columnExists = await _db.Columns.AnyAsync(c => c.Id == request.ColumnId && c.BoardId == request.BoardId, cancellationToken);
        if (!boardExists || !columnExists)
        {
            throw new BoardOrColumnNotFoundInTenantException();
        }

        var maxOrder = await _db.Tasks
            .Where(t => t.BoardId == request.BoardId && t.ColumnId == request.ColumnId)
            .Select(t => (int?)t.OrderIndex)
            .MaxAsync(cancellationToken) ?? -1;

        var task = new Domain.Entities.Task
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            BoardId = request.BoardId,
            ColumnId = request.ColumnId,
            Title = request.Request.Title.Trim(),
            Description = request.Request.Description,
            Status = "Open",
            Priority = string.IsNullOrWhiteSpace(request.Request.Priority) ? "Medium" : request.Request.Priority,
            AssigneeUserId = request.Request.AssigneeUserId,
            ReporterUserId = request.ReporterUserId,
            DueDate = request.Request.DueDate,
            OrderIndex = maxOrder + 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken); // SQL Server bumps RowVersion

        var dto = TaskMappingExtensions.ToDto(task);

        await _audit.WriteAsync("task.created", beforeJson: null, afterJson: dto, source: "api", cancellationToken);
        await _outbox.WriteAsync("task.created", new
        {
            boardId = task.BoardId,
            columnId = task.ColumnId,
            taskId = task.Id,
            organizationId = task.OrganizationId,
            task = dto
        }, cancellationToken);

        await tx.CommitAsync(cancellationToken);
        return dto;
    }
}
