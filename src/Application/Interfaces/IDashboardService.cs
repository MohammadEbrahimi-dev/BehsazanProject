using Behsazan.Application.DTOs;

namespace Behsazan.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecentProjectDto>> GetRecentProjectsAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecentInvoiceDto>> GetRecentInvoicesAsync(int count, CancellationToken cancellationToken = default);
}
