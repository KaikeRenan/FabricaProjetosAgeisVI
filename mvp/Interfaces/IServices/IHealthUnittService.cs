using mvp.DTOs;

namespace mvp.Interfaces.IServices
{
    public interface IHealthUnittService
    {
        Task<List<HealthUnitResponseDTO>> GetAllAsync();
        Task<HealthUnitResponseDTO?> GetByIdAsync(Guid Id);
        Task<HealthUnitResponseDTO> CreateAsync(HealthUnitCreateDTO dto);
        Task<HealthUnitResponseDTO> UpdateAsync(Guid Id, HealthUnitUpdateDTO dto);
        Task DeleteAsync(Guid Id);
    }
}
