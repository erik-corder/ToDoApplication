using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Kanban.Application.Boards;
using Kanban.Application.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kanban.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantMember")]
[Route("api/workspaces/{workspaceId:guid}")]
public class BoardsController : ControllerBase
{
    private readonly CreateBoardHandler _createBoardHandler;
    private readonly GetSidebarHandler _getSidebarHandler;

    public BoardsController(
        CreateBoardHandler createBoardHandler,
        GetSidebarHandler getSidebarHandler)
    {
        _createBoardHandler = createBoardHandler;
        _getSidebarHandler = getSidebarHandler;
    }

    [HttpPost("boards")]
    [ProducesResponseType(typeof(CreateBoardResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreateBoardResponse>> CreateBoard(
        Guid workspaceId,
        [FromBody] CreateBoardRequest request,
        CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _createBoardHandler.HandleAsync(userId.Value, workspaceId, request, ct);
        if (result.IsValidationFailure)
            return BadRequest(new ValidationProblemResponse(result.Errors));
        if (result.IsForbidden)
            return Forbid();

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet("sidebar")]
    [ProducesResponseType(typeof(SidebarResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SidebarResponse>> GetSidebar(Guid workspaceId, CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _getSidebarHandler.HandleAsync(userId.Value, workspaceId, ct);
        if (result.IsForbidden) return Forbid();

        return Ok(result.Value);
    }

    private Guid? CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
