using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kanban.Application.Audit;
using Kanban.Application.Authorizers;
using Kanban.Application.Outbox;
using Kanban.Application.Validation;
using Kanban.Domain.Entities;
using Kanban.Infrastructure.Persistence;

namespace Kanban.Application.Boards;

public sealed class CreateBoardHandler
{
    private static readonly (string Name, int OrderIndex)[] DefaultColumns =
    {
        ("To Do", 0),
        ("In Progress", 1),
        ("Done", 2)
    };

    private readonly AppDbContext _db;
    private readonly CreateBoardRequestValidator _validator;
    private readonly ITenantAuthorizer _tenantAuthorizer;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;

    public CreateBoardHandler(
        AppDbContext db,
        CreateBoardRequestValidator validator,
        ITenantAuthorizer tenantAuthorizer,
        IAuditWriter audit,
        IOutboxWriter outbox)
    {
        _db = db;
        _validator = validator;
        _tenantAuthorizer = tenantAuthorizer;
        _audit = audit;
        _outbox = outbox;
    }

    public async Task<CreateBoardCommandResult> HandleAsync(
        Guid userId,
        Guid workspaceId,
        CreateBoardRequest request,
        CancellationToken ct)
    {
        var errors = _validator.Validate(request);
        if (errors.Count > 0)
            return CreateBoardCommandResult.Validation(errors);

        var workspace = await _db.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId, ct);
        if (workspace == null)
            return CreateBoardCommandResult.Forbidden();

        var isMember = await _tenantAuthorizer.IsActiveMemberAsync(userId, workspace.OrganizationId, ct);
        if (!isMember)
            return CreateBoardCommandResult.Forbidden();

        var now = DateTime.UtcNow;
        var board = new Board
        {
            Id = Guid.NewGuid(),
            OrganizationId = workspace.OrganizationId,
            WorkspaceId = workspace.Id,
            Name = request.Name.Trim(),
            ArchivedAt = null,
            CreatedAt = now
        };

        var columns = DefaultColumns.Select(c => new Domain.Entities.Column
        {
            Id = Guid.NewGuid(),
            OrganizationId = workspace.OrganizationId,
            BoardId = board.Id,
            Name = c.Name,
            OrderIndex = c.OrderIndex,
            WipLimit = null,
            CreatedAt = now
        }).ToList();

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        _db.Boards.Add(board);
        _db.Columns.AddRange(columns);

        await _audit.WriteAsync("Board.Created", "Board", board.Id,
            new { id = board.Id, name = board.Name, workspaceId = board.WorkspaceId }, ct);

        await _outbox.EnqueueAsync(new OutboxEnvelope(
            OrganizationId: workspace.OrganizationId,
            Action: "Board.Created",
            AggregateId: board.Id,
            Payload: new { id = board.Id, name = board.Name, workspaceId = board.WorkspaceId }), ct);

        foreach (var column in columns)
        {
            await _audit.WriteAsync("Column.Created", "Column", column.Id,
                new { id = column.Id, name = column.Name, orderIndex = column.OrderIndex }, ct);
            await _outbox.EnqueueAsync(new OutboxEnvelope(
                OrganizationId: workspace.OrganizationId,
                Action: "Column.Created",
                AggregateId: column.Id,
                Payload: new { id = column.Id, name = column.Name, orderIndex = column.OrderIndex }), ct);
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return CreateBoardCommandResult.Success(new CreateBoardResponse(
            board.Id,
            columns.Select(c => new BoardColumnDto(c.Id, c.Name, c.OrderIndex)).ToList()));
    }
}
