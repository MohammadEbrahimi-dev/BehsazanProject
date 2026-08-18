using Behsazan.Application.DTOs;

namespace Behsazan.Application.Interfaces;

public interface IInvoicePdfService
{
    Task<FileDownloadDto?> ExportAsync(int invoiceId, CancellationToken cancellationToken = default);
}
