using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Tasks.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Tasks.Queries;

public record GetTaskQuery(Guid TaskId, Guid OrganizationId) : IRequest<TaskDto?>;

public class GetTaskQueryHandler : IRequestHandler<GetTaskQuery, TaskDto?>
{
    private readonly IAppDbContext _db;

    public GetTaskQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<TaskDto?> Handle(GetTaskQuery request, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task is null || task.OrganizationId != request.OrganizationId)
        {
            return null;
        }

        return TaskMappingExtensions.ToDto(task);
    }
}
