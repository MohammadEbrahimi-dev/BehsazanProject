using Behsazan.Application.DTOs;

namespace Behsazan.Application.Interfaces;

public interface IInvoiceService
{
    Task<PagedResultDto<InvoiceListItemDto>> GetPagedAsync(
        InvoiceQueryDto query,
        CancellationToken cancellationToken = default);

    Task<InvoiceDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<InvoiceFormDto?> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvoiceListItemDto>> GetByProjectAsync(
        int projectId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerLookupDto>> SearchCustomersAsync(
        string? searchTerm,
        int take = 20,
        CancellationToken cancellationToken = default);

    Task<CustomerLookupDto?> GetCustomerLookupAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectLookupDto>> SearchProjectsAsync(
        string? searchTerm,
        int? customerId = null,
        int take = 20,
        CancellationToken cancellationToken = default);

    Task<ProjectLookupDto?> GetProjectLookupAsync(
        int projectId,
        CancellationToken cancellationToken = default);

    Task<OperationResultDto> CreateAsync(
        InvoiceFormDto form,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResultDto> UpdateAsync(
        InvoiceFormDto form,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResultDto> DeleteAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default);
}
