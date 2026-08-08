using JobSearchAggregator.Application.Common.Interfaces;
using Mapster;
using MediatR;

namespace JobSearchAggregator.Application.Settings.Queries;

/// <summary>
/// Fetches the user's current job search preferences and app configuration.
/// </summary>
public record GetUserSettingsQuery : IRequest<UserSettingsDto>;

public class GetUserSettingsQueryHandler : IRequestHandler<GetUserSettingsQuery, UserSettingsDto>
{
    private readonly IUserSettingsRepository _userSettingsRepository;

    public GetUserSettingsQueryHandler(IUserSettingsRepository userSettingsRepository)
    {
        _userSettingsRepository = userSettingsRepository;
    }

    public async Task<UserSettingsDto> Handle(GetUserSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _userSettingsRepository.GetCurrentAsync(cancellationToken);
        return settings.Adapt<UserSettingsDto>();
    }
}
