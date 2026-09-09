using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kanban.Application.Outbox;

public sealed record OutboxEnvelope(Guid OrganizationId, string Action, Guid AggregateId, object Payload);

public interface IOutboxWriter
{
    Task EnqueueAsync(OutboxEnvelope envelope, CancellationToken ct);
}
