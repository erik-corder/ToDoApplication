using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kanban.Domain.Entities;
using Kanban.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kanban.Application.Onboarding;

public sealed class GetOnboardingStatusHandler
{
    private readonly AppDbContext _db;

    public GetOnboardingStatusHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OnboardingStatusResponse> HandleAsync(Guid userId, CancellationToken ct)
    {
        var activeMembership = await _db.Memberships
            .IgnoreQueryFilters()
            .Where(m => m.UserId == userId && m.Status == MembershipStatus.Active)
            .OrderBy(m => m.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (activeMembership == null)
            return new OnboardingStatusResponse(NeedsOnboarding: true, UserId: userId);

        var defaultWorkspace = await _db.Workspaces
            .IgnoreQueryFilters()
            .Where(w => w.OrganizationId == activeMembership.OrganizationId)
            .OrderBy(w => w.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return new OnboardingStatusResponse(
            NeedsOnboarding: false,
            UserId: userId,
            ActiveOrganizationId: activeMembership.OrganizationId,
            ActiveWorkspaceId: defaultWorkspace?.Id);
    }
}
