using Behsazan.Application.DTOs;

namespace Behsazan.Application.Interfaces;

public interface IProjectFinancialReportService
{
    Task<FileDownloadDto?> ExportExcelAsync(int projectId, CancellationToken cancellationToken = default);

    Task<FileDownloadDto?> ExportPdfAsync(int projectId, CancellationToken cancellationToken = default);
}
