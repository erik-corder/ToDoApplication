using System.Threading;
using System.Threading.Tasks;
using Api.Common;
using Api.Common.Concurrency;
using Application.Common.Concurrency;
using Application.Tasks.Commands;
using Application.Tasks.Dtos;
using Application.Tasks.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITenantAuthorizer _tenantAuthorizer;
    private readonly IETagService _etagService;

    public TasksController(IMediator mediator, ITenantAuthorizer tenantAuthorizer, IETagService etagService)
    {
        _mediator = mediator;
        _tenantAuthorizer = tenantAuthorizer;
        _etagService = etagService;
    }

    /// <summary>
    /// Returns a single task for the authenticated tenant. Returns 404 for missing
    /// or cross-tenant tasks. Sets the ETag header to the base64 RowVersion.
    /// </summary>
    [HttpGet("{taskId:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> GetTask(
        [FromRoute] Guid taskId,
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

        var task = await _mediator.Send(new GetTaskQuery(taskId, organizationId), cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        SetETag(task.RowVersion);
        return Ok(task);
    }

    /// <summary>
    /// Edits any mutable field on a task. Honors If-Match for optimistic concurrency
    /// and returns 409 with the latest server snapshot on a RowVersion mismatch.
    /// </summary>
    [HttpPut("{taskId:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ConflictResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> UpdateTask(
        [FromRoute] Guid taskId,
        [FromBody] UpdateTaskRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
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

        byte[]? expectedRowVersion = null;
        if (!string.IsNullOrWhiteSpace(ifMatch))
        {
            try
            {
                expectedRowVersion = _etagService.Decode(ifMatch);
            }
            catch
            {
                return BadRequest(ProblemDetailsFactory.CreateValidationProblemDetails(HttpContext, "If-Match header is not a valid base64 RowVersion."));
            }
        }
        else
        {
            // Surface a soft warning: server still bumps RowVersion but cannot detect concurrent edits.
            Response.Headers["X-Concurrency-Warning"] = "missing-if-match";
        }

        try
        {
            var updated = await _mediator.Send(
                new UpdateTaskCommand(taskId, organizationId, request, expectedRowVersion),
                cancellationToken);

            SetETag(updated.RowVersion);
            return Ok(updated);
        }
        catch (TaskNotFoundInTenantException)
        {
            return NotFound();
        }
        catch (ConcurrencyException ex)
        {
            SetETag(ex.LatestRowVersion);
            return Conflict(new ConflictResponse
            {
                Message = "This task was updated by someone else.",
                Latest = ex.Latest
            });
        }
    }

    /// <summary>
    /// Hard-deletes a task. Role gate: creator OR organization admin/owner.
    /// </summary>
    [HttpDelete("{taskId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ConflictResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(
        [FromRoute] Guid taskId,
        [FromHeader(Name = "If-Match")] string? ifMatch,
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

        byte[]? expectedRowVersion = null;
        if (!string.IsNullOrWhiteSpace(ifMatch))
        {
            try
            {
                expectedRowVersion = _etagService.Decode(ifMatch);
            }
            catch
            {
                return BadRequest();
            }
        }

        try
        {
            await _mediator.Send(
                new DeleteTaskCommand(taskId, organizationId, User.GetUserId(), User.IsInRole("Admin") || User.IsInRole("Owner"), expectedRowVersion),
                cancellationToken);
            return NoContent();
        }
        catch (TaskNotFoundInTenantException)
        {
            return NotFound();
        }
        catch (DeleteNotPermittedException)
        {
            return Forbid();
        }
        catch (ConcurrencyException ex)
        {
            SetETag(ex.LatestRowVersion);
            return Conflict(new ConflictResponse
            {
                Message = "This task was updated by someone else.",
                Latest = ex.Latest
            });
        }
    }

    private void SetETag(byte[] rowVersion)
    {
        Response.Headers.ETag = $"\"{_etagService.Encode(rowVersion)}\"";
    }
}
