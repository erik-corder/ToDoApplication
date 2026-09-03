using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Concurrency;
using Application.Common.Interfaces;
using Application.Tasks.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Tasks.Commands;

public record UpdateTaskCommand(
    Guid TaskId,
    Guid OrganizationId,
    UpdateTaskRequest Request,
    byte[]? ExpectedRowVersion) : IRequest<TaskDto>;

public class TaskNotFoundInTenantException : Exception { }

public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, TaskDto>
{
    private readonly IAppDbContext _db;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;

    public UpdateTaskCommandHandler(IAppDbContext db, IAuditWriter audit, IOutboxWriter outbox)
    {
        _db = db;
        _audit = audit;
        _outbox = outbox;
    }

    public async Task<TaskDto> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _db.BeginTransactionAsync(cancellationToken);

        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);
        if (task is null || task.OrganizationId != request.OrganizationId)
        {
            throw new TaskNotFoundInTenantException();
        }

        // If-Match concurrency check
        if (request.ExpectedRowVersion is not null && !task.RowVersion.SequenceEqual(request.ExpectedRowVersion))
        {
            var latestDto = TaskMappingExtensions.ToDto(task);
            throw new ConcurrencyException(
                "This task was updated by someone else.",
                latestDto,
                task.RowVersion.ToArray());
        }

        var beforeDto = TaskMappingExtensions.ToDto(task);

        task.Title = request.Request.Title.Trim();
        task.Description = request.Request.Description;
        task.AssigneeUserId = request.Request.AssigneeUserId;
        task.DueDate = request.Request.DueDate;
        task.Priority = string.IsNullOrWhiteSpace(request.Request.Priority) ? "Medium" : request.Request.Priority;
        task.Status = string.IsNullOrWhiteSpace(request.Request.Status) ? "Open" : request.Request.Status;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        // ReporterUserId and OrganizationId intentionally not mutable here.

        await _db.SaveChangesAsync(cancellationToken); // SQL Server bumps RowVersion

        var afterDto = TaskMappingExtensions.ToDto(task);

        await _audit.WriteAsync("task.updated", beforeDto, afterDto, "api", cancellationToken);
        await _outbox.WriteAsync("task.updated", new
        {
            boardId = task.BoardId,
            columnId = task.ColumnId,
            taskId = task.Id,
            organizationId = task.OrganizationId,
            task = afterDto
        }, cancellationToken);

        await tx.CommitAsync(cancellationToken);
        return afterDto;
    }
}
