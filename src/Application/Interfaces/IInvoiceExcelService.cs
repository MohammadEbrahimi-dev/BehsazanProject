using Behsazan.Application.DTOs;

namespace Behsazan.Application.Interfaces;

public interface IInvoiceExcelService
{
    Task<FileDownloadDto?> ExportAsync(int invoiceId, CancellationToken cancellationToken = default);

    Task<InvoiceExcelParseResultDto> ParseImportAsync(
        Stream stream,
        CancellationToken cancellationToken = default);
}
