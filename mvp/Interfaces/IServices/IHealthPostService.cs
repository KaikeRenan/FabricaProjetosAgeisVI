using mvp.DTOs;

namespace mvp.Interfaces.IServices
{
    public interface IHealthPostService
    {
        Task<List<HealthPostResponseDTO>> GetAllAsync();
        Task<HealthPostResponseDTO?> GetByIdAsync(Guid Id);
        Task<HealthPostResponseDTO> CreateAsync(HealthPostCreateDTO dto);
        Task<HealthPostResponseDTO> UpdateAsync(Guid Id, HealthPostUpdateDTO dto);
        Task DeleteAsync(Guid Id);
    }
}
