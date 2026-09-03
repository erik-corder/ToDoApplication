using System.Threading;
using System.Threading.Tasks;
using Api.Common;
using Application.Tasks.Dtos;
using Application.Tasks.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/boards")]
public class BoardsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITenantAuthorizer _tenantAuthorizer;

    public BoardsController(IMediator mediator, ITenantAuthorizer tenantAuthorizer)
    {
        _mediator = mediator;
        _tenantAuthorizer = tenantAuthorizer;
    }

    /// <summary>
    /// Returns the board metadata, its columns ordered by OrderIndex, and the tasks
    /// already grouped per column for kanban rendering. All reads are tenant-scoped
    /// via the global OrganizationId query filter; a board belonging to another
    /// organization returns 404 (not 403) to avoid existence leakage.
    /// </summary>
    [HttpGet("{boardId:guid}")]
    [ProducesResponseType(typeof(BoardDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoardDetailDto>> GetBoard(
        [FromRoute] Guid boardId,
        CancellationToken cancellationToken)
    {
        var organizationId = User.GetOrganizationId();
        if (organizationId == Guid.Empty)
        {
            return Unauthorized();
        }

        var isMember = await _tenantAuthorizer.IsMemberAsync(organizationId, User.GetUserId(), cancellationToken);
        if (!isMember)
        {
            return NotFound();
        }

        var result = await _mediator.Send(new GetBoardDetailQuery(boardId, organizationId), cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}
