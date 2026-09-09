using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kanban.Application.Authorizers;
using Kanban.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kanban.Application.Boards;

public sealed class GetSidebarHandler
{
    private readonly AppDbContext _db;
    private readonly ITenantAuthorizer _tenantAuthorizer;

    public GetSidebarHandler(AppDbContext db, ITenantAuthorizer tenantAuthorizer)
    {
        _db = db;
        _tenantAuthorizer = tenantAuthorizer;
    }

    public async Task<SidebarCommandResult> HandleAsync(Guid userId, Guid workspaceId, CancellationToken ct)
    {
        var workspace = await _db.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId, ct);
        if (workspace == null)
            return SidebarCommandResult.Forbidden();

        var isMember = await _tenantAuthorizer.IsActiveMemberAsync(userId, workspace.OrganizationId, ct);
        if (!isMember)
            return SidebarCommandResult.Forbidden();

        var boards = await _db.Boards
            .Where(b => b.WorkspaceId == workspaceId && b.ArchivedAt == null)
            .OrderBy(b => b.CreatedAt)
            .Select(b => new SidebarBoardDto(b.Id, b.Name, b.ArchivedAt))
            .ToListAsync(ct);

        return SidebarCommandResult.Success(new SidebarResponse(workspaceId, boards));
    }
}
