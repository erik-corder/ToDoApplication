using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Kanban.Application.Onboarding;
using Kanban.Application.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kanban.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class OnboardingController : ControllerBase
{
    private readonly GetOnboardingStatusHandler _getOnboardingStatusHandler;
    private readonly CreateOrganizationHandler _createOrganizationHandler;

    public OnboardingController(
        GetOnboardingStatusHandler getOnboardingStatusHandler,
        CreateOrganizationHandler createOrganizationHandler)
    {
        _getOnboardingStatusHandler = getOnboardingStatusHandler;
        _createOrganizationHandler = createOrganizationHandler;
    }

    [HttpGet("me/onboarding-status")]
    [ProducesResponseType(typeof(OnboardingStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<OnboardingStatusResponse>> GetOnboardingStatus(CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var response = await _getOnboardingStatusHandler.HandleAsync(userId.Value, ct);
        return Ok(response);
    }

    [HttpPost("organizations")]
    [ProducesResponseType(typeof(CreateOrganizationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateOrganizationResponse>> CreateOrganization(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _createOrganizationHandler.HandleAsync(userId.Value, request, ct);
        if (result.IsValidationFailure)
            return BadRequest(new ValidationProblemResponse(result.Errors));
        if (result.IsSlugConflict)
            return Conflict(new ValidationProblemResponse(result.Errors));

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    private Guid? CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
