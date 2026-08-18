using Behsazan.Application.DTOs;

namespace Behsazan.Application.Interfaces;

public interface IProjectService
{
    Task<PagedResultDto<ProjectListItemDto>> GetPagedAsync(
        ProjectQueryDto query,
        CancellationToken cancellationToken = default);

    Task<ProjectDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ProjectFormDto?> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectListItemDto>> GetByCustomerAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerLookupDto>> SearchCustomersAsync(
        string? searchTerm,
        int take = 20,
        CancellationToken cancellationToken = default);

    Task<CustomerLookupDto?> GetCustomerLookupAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<OperationResultDto> CreateAsync(
        ProjectFormDto form,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResultDto> UpdateAsync(
        ProjectFormDto form,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResultDto> DeleteAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default);
}
