using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kanban.Application.Audit;

public interface IAuditWriter
{
    Task WriteAsync(string action, string targetType, Guid targetId, object after, CancellationToken ct);
}
