using Behsazan.Application.DTOs;
using Behsazan.Application.Enums;

namespace Behsazan.Application.Interfaces;

public interface IFinancialReportService
{
    Task<FinancialDashboardDto> GetDashboardAsync(
        DateTime? from = null,
        DateTime? to = null,
        FinancialTrendGranularity granularity = FinancialTrendGranularity.Month,
        CancellationToken cancellationToken = default);
}
