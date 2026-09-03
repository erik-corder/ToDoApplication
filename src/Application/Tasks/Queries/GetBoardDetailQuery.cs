using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Tasks.Dtos;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Tasks.Queries;

public record GetBoardDetailQuery(Guid BoardId, Guid OrganizationId) : IRequest<BoardDetailDto?>;

public class GetBoardDetailQueryHandler : IRequestHandler<GetBoardDetailQuery, BoardDetailDto?>
{
    private readonly IAppDbContext _db;

    public GetBoardDetailQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<BoardDetailDto?> Handle(GetBoardDetailQuery request, CancellationToken cancellationToken)
    {
        // Board exists in tenant? (global query filter enforces OrganizationId)
        var board = await _db.Boards
            .AsNoTracking()
            .Where(b => b.Id == request.BoardId)
            .Select(b => new { b.Id, b.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (board is null)
        {
            return null;
        }

        var columns = await _db.Columns
            .AsNoTracking()
            .Where(c => c.BoardId == request.BoardId)
            .OrderBy(c => c.OrderIndex)
            .Select(c => new { c.Id, c.Name, c.OrderIndex })
            .ToListAsync(cancellationToken);

        var tasks = await _db.Tasks
            .AsNoTracking()
            .Where(t => t.BoardId == request.BoardId)
            .OrderBy(t => t.ColumnId)
            .ThenBy(t => t.OrderIndex)
            .ToListAsync(cancellationToken);

        var dto = new BoardDetailDto
        {
            Id = board.Id,
            Name = board.Name
        };

        foreach (var col in columns)
        {
            dto.Columns.Add(new BoardColumnDto
            {
                Id = col.Id,
                Name = col.Name,
                OrderIndex = col.OrderIndex,
                Tasks = tasks
                    .Where(t => t.ColumnId == col.Id)
                    .Select(TaskMappingExtensions.ToDto)
                    .ToList()
            });
        }

        return dto;
    }
}
