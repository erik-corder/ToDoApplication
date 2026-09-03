using System;
using System.Threading;
using System.Threading.Tasks;
using Api.Common;
using Application.Common.Concurrency;
using Application.Tasks.Commands;
using Application.Tasks.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/boards/{boardId:guid}/columns/{columnId:guid}/tasks")]
public class ColumnTasksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITenantAuthorizer _tenantAuthorizer;
    private readonly IETagService _etagService;

    public ColumnTasksController(IMediator mediator, ITenantAuthorizer tenantAuthorizer, IETagService etagService)
    {
        _mediator = mediator;
        _tenantAuthorizer = tenantAuthorizer;
        _etagService = etagService;
    }

    /// <summary>
    /// Creates a new task in the specified column. Board/column must belong to the
    /// caller's organization (else 404). Returns 201 with Location and ETag.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> CreateTask(
        [FromRoute] Guid boardId,
        [FromRoute] Guid columnId,
        [FromBody] CreateTaskRequest request,
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

        try
        {
            var created = await _mediator.Send(
                new CreateTaskCommand(boardId, columnId, organizationId, User.GetUserId(), request),
                cancellationToken);

            Response.Headers.ETag = $"\"{_etagService.Encode(created.RowVersion)}\"";
            return CreatedAtAction(
                nameof(TasksController.GetTask),
                "Tasks",
                new { taskId = created.Id },
                created);
        }
        catch (BoardOrColumnNotFoundInTenantException)
        {
            return NotFound();
        }
    }
}
