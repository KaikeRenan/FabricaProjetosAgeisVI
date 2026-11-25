using mvp.DTOs.ExpirationAlert;

namespace mvp.Interfaces.IServices
{
    public interface IExpirationAlertService : IBaseService<ExpirationAlertResponseDTO, ExpirationAlertCreateDTO, ExpirationAlertUpdateDTO>
    {
        Task<List<ExpirationAlertResponseDTO>> GetAllWithDetailsAsync();
        Task<ExpirationAlertResponseDTO?> GetByIdWithDetailsAsync(Guid id);
        Task<ExpirationAlertGenerationResultDTO> GenerateAsync(int daysAhead, bool autoResolve, CancellationToken ct);
    }

}
