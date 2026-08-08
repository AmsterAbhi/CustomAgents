using JobSearchAggregator.Application.Settings;
using JobSearchAggregator.Application.Settings.Commands;
using JobSearchAggregator.Application.Settings.Queries;
using JobSearchAggregator.Shared.Contracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace JobSearchAggregator.Api.Controllers;

/// <summary>
/// Read/write access to the user's job search preferences and app
/// configuration (Locations, Salary, Experience, Preferred Roles/Technologies,
/// Notification Threshold, Scheduler interval, Enabled Providers).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ISender _sender;

    public SettingsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<UserSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> Get(CancellationToken cancellationToken)
    {
        var settings = await _sender.Send(new GetUserSettingsQuery(), cancellationToken);
        return Ok(ApiResponse<UserSettingsDto>.Ok(settings));
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<UserSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> Update(
        [FromBody] UpdateUserSettingsCommand command,
        CancellationToken cancellationToken)
    {
        var settings = await _sender.Send(command, cancellationToken);
        return Ok(ApiResponse<UserSettingsDto>.Ok(settings, "Settings updated."));
    }
}
