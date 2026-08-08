using JobSearchAggregator.Application.Scheduler;
using JobSearchAggregator.Application.Scheduler.Commands;
using JobSearchAggregator.Application.Scheduler.Queries;
using JobSearchAggregator.Domain.Enums;
using JobSearchAggregator.Domain.Exceptions;
using JobSearchAggregator.Shared.Contracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace JobSearchAggregator.Api.Controllers;

/// <summary>
/// Manual/automatic scheduler run triggers, single-provider retry, and
/// scheduler run history/status reads (Phase 2 - Scheduler & Provider
/// Architecture).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SchedulerController : ControllerBase
{
    private readonly ISender _sender;

    public SchedulerController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("run")]
    [ProducesResponseType(typeof(ApiResponse<SchedulerRunDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SchedulerRunDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<SchedulerRunDto>>> Run(CancellationToken cancellationToken)
    {
        try
        {
            var run = await _sender.Send(new RunSchedulerCommand(SchedulerTriggerType.Manual), cancellationToken);
            return Ok(ApiResponse<SchedulerRunDto>.Ok(run!, "Scheduler run completed."));
        }
        catch (SchedulerRunInProgressException ex)
        {
            return Conflict(ApiResponse<SchedulerRunDto>.Fail(ex.Message));
        }
    }

    [HttpPost("runs/{id:guid}/retry-provider/{providerName}")]
    [ProducesResponseType(typeof(ApiResponse<SchedulerRunDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SchedulerRunDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SchedulerRunDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<SchedulerRunDto>>> RetryProvider(
        Guid id,
        string providerName,
        CancellationToken cancellationToken)
    {
        try
        {
            var run = await _sender.Send(new RetryProviderCommand(id, providerName), cancellationToken);
            return Ok(ApiResponse<SchedulerRunDto>.Ok(run, "Provider retry completed."));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ApiResponse<SchedulerRunDto>.Fail(ex.Message));
        }
        catch (SchedulerRunInProgressException ex)
        {
            return Conflict(ApiResponse<SchedulerRunDto>.Fail(ex.Message));
        }
    }

    [HttpGet("runs")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SchedulerRunDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<SchedulerRunDto>>>> GetRuns(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var runs = await _sender.Send(new GetSchedulerRunsQuery(request), cancellationToken);
        return Ok(ApiResponse<PagedResult<SchedulerRunDto>>.Ok(runs));
    }

    [HttpGet("runs/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SchedulerRunDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SchedulerRunDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SchedulerRunDto>>> GetRunById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var run = await _sender.Send(new GetSchedulerRunByIdQuery(id), cancellationToken);
            return Ok(ApiResponse<SchedulerRunDto>.Ok(run));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ApiResponse<SchedulerRunDto>.Fail(ex.Message));
        }
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<SchedulerStatusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SchedulerStatusDto>>> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _sender.Send(new GetSchedulerStatusQuery(), cancellationToken);
        return Ok(ApiResponse<SchedulerStatusDto>.Ok(status));
    }
}
