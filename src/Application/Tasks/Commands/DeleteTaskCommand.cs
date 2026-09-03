using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Concurrency;
using Application.Common.Interfaces;
using Application.Tasks.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Tasks.Commands;

public class DeleteNotPermittedException : Exception { }

public record DeleteTaskCommand(
    Guid TaskId,
    Guid OrganizationId,
    Guid UserId,
    bool IsAdminOrOwner,
    byte[]? ExpectedRowVersion) : IRequest;

public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand>
{
    private readonly IAppDbContext _db;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;

    public DeleteTaskCommandHandler(IAppDbContext db, IAuditWriter audit, IOutboxWriter outbox)
    {
        _db = db;
        _audit = audit;
        _outbox = outbox;
    }

    public async Task Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _db.BeginTransactionAsync(cancellationToken);

        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);
        if (task is null || task.OrganizationId != request.OrganizationId)
        {
            throw new TaskNotFoundInTenantException();
        }

        // Role gate: creator OR admin/owner (per HLD)
        var isCreator = task.ReporterUserId == request.UserId;
        if (!isCreator && !request.IsAdminOrOwner)
        {
            throw new DeleteNotPermittedException();
        }

        if (request.ExpectedRowVersion is not null && !task.RowVersion.SequenceEqual(request.ExpectedRowVersion))
        {
            var latestDto = TaskMappingExtensions.ToDto(task);
            throw new ConcurrencyException(
                "This task was updated by someone else.",
                latestDto,
                task.RowVersion.ToArray());
        }

        var beforeDto = TaskMappingExtensions.ToDto(task);

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync("task.deleted", beforeDto, afterJson: null, source: "api", cancellationToken);
        await _outbox.WriteAsync("task.deleted", new
        {
            boardId = task.BoardId,
            columnId = task.ColumnId,
            taskId = task.Id,
            organizationId = task.OrganizationId
        }, cancellationToken);

        await tx.CommitAsync(cancellationToken);
    }
}
