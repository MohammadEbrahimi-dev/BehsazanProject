using Behsazan.Application.DTOs;

namespace Behsazan.Application.Interfaces;

public interface IDepositService
{
    Task<PagedResultDto<DepositListItemDto>> GetPagedAsync(
        DepositQueryDto query,
        CancellationToken cancellationToken = default);

    Task<DepositFormDto?> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectLookupDto>> SearchProjectsAsync(
        string? searchTerm,
        int take = 20,
        CancellationToken cancellationToken = default);

    Task<ProjectLookupDto?> GetProjectLookupAsync(
        int projectId,
        CancellationToken cancellationToken = default);

    Task<OperationResultDto> CreateAsync(
        DepositFormDto form,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResultDto> UpdateAsync(
        DepositFormDto form,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResultDto> DeleteAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default);
}
