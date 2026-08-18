using Behsazan.Application.DTOs;

namespace Behsazan.Application.Interfaces;

public interface IProjectLedgerService
{
    Task<ProjectLedgerDto?> GetByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken = default);
}
