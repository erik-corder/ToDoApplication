using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kanban.Application.Audit;
using Kanban.Application.Outbox;
using Kanban.Application.Validation;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;
using Kanban.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kanban.Application.Onboarding;

public sealed class CreateOrganizationHandler
{
    private readonly AppDbContext _db;
    private readonly CreateOrganizationRequestValidator _validator;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;

    public CreateOrganizationHandler(
        AppDbContext db,
        CreateOrganizationRequestValidator validator,
        IAuditWriter audit,
        IOutboxWriter outbox)
    {
        _db = db;
        _validator = validator;
        _audit = audit;
        _outbox = outbox;
    }

    public async Task<OnboardingCommandResult> HandleAsync(
        Guid userId,
        CreateOrganizationRequest request,
        CancellationToken ct)
    {
        var errors = _validator.Validate(request);
        if (errors.Count > 0)
            return OnboardingCommandResult.Validation(errors);

        // Pre-write assertion: only one Active membership is allowed per user.
        var hasActive = await _db.Memberships
            .IgnoreQueryFilters()
            .AnyAsync(m => m.UserId == userId && m.Status == MembershipStatus.Active, ct);
        if (hasActive)
            return OnboardingCommandResult.Conflict(new Dictionary<string, string>
            {
                ["user"] = "User already has an active organization."
            });

        var now = DateTime.UtcNow;
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = request.Slug,
            Status = OrganizationStatus.Active,
            CreatedAt = now
        };

        var membership = new Membership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = organization.Id,
            Role = MembershipRole.Owner,
            Status = MembershipStatus.Active,
            InvitedByUserId = null,
            CreatedAt = now
        };

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Name = "Default",
            Slug = "default",
            CreatedAt = now
        };

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            _db.Organizations.Add(organization);
            _db.Memberships.Add(membership);
            _db.Workspaces.Add(workspace);

            await _audit.WriteAsync("Organization.Created", "Organization", organization.Id,
                new { id = organization.Id, name = organization.Name, slug = organization.Slug }, ct);
            await _audit.WriteAsync("Membership.Created", "Membership", membership.Id,
                new { userId = membership.UserId, organizationId = membership.OrganizationId, role = "Owner" }, ct);
            await _audit.WriteAsync("Workspace.Created", "Workspace", workspace.Id,
                new { id = workspace.Id, name = workspace.Name, slug = workspace.Slug }, ct);

            await _outbox.EnqueueAsync(new OutboxEnvelope(
                OrganizationId: organization.Id,
                Action: "Organization.Created",
                AggregateId: organization.Id,
                Payload: new { id = organization.Id, name = organization.Name, slug = organization.Slug }), ct);
            await _outbox.EnqueueAsync(new OutboxEnvelope(
                OrganizationId: organization.Id,
                Action: "Membership.Created",
                AggregateId: membership.Id,
                Payload: new { userId, organizationId = organization.Id, role = "Owner" }), ct);
            await _outbox.EnqueueAsync(new OutboxEnvelope(
                OrganizationId: organization.Id,
                Action: "Workspace.Created",
                AggregateId: workspace.Id,
                Payload: new { id = workspace.Id, name = workspace.Name, slug = workspace.Slug }), ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (IsSlugUniqueViolation(ex))
        {
            await tx.RollbackAsync(ct);
            return OnboardingCommandResult.Conflict(new Dictionary<string, string>
            {
                ["slug"] = "Slug already in use."
            });
        }

        return OnboardingCommandResult.Success(new CreateOrganizationResponse(
            organization.Id, workspace.Id, membership.Id));
    }

    private static bool IsSlugUniqueViolation(DbUpdateException ex)
    {
        // HLD enforces unique index on Organization.Slug. SQL Server error number 2627 / 2601.
        var inner = ex.InnerException?.Message ?? string.Empty;
        return inner.Contains("IX_Organizations_Slug", StringComparison.OrdinalIgnoreCase)
            || inner.Contains("UQ_Organizations_Slug", StringComparison.OrdinalIgnoreCase)
            || inner.Contains("Slug", StringComparison.OrdinalIgnoreCase) && inner.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }
}
