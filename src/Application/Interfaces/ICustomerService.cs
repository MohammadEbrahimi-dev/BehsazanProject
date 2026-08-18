using Behsazan.Application.DTOs;

namespace Behsazan.Application.Interfaces;

public interface ICustomerService
{
    Task<PagedResultDto<CustomerListItemDto>> GetPagedAsync(
        CustomerQueryDto query,
        CancellationToken cancellationToken = default);

    Task<CustomerDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<CustomerFormDto?> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResultDto> CreateAsync(
        CustomerFormDto form,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResultDto> UpdateAsync(
        CustomerFormDto form,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResultDto> DeleteAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<bool> IsNationalCodeAvailableAsync(
        string nationalCode,
        int? excludeCustomerId = null,
        CancellationToken cancellationToken = default);
}
