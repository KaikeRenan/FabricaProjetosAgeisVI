using mvp.DTOs.ExpirationAlert;
using mvp.Entities;

namespace mvp.Interfaces.IRepositories
{
    public interface IExpirationAlertRepository : IBaseRepository<ExpirationAlert>
    {
        Task<List<ExpirationAlertResponseDTO>> GetAllWithDetailsAsync();
        Task<ExpirationAlertResponseDTO?> GetByIdWithDetailsAsync(Guid id);
        Task<ExpirationAlertGenerationResultDTO> GenerateAsync(int daysAhead, bool autoResolve, CancellationToken ct);
    }
}
