using mvp.DTOs;

namespace mvp.Interfaces.IServices
{
    public interface IHealthUnitService : IBaseService<HealthUnitResponseDTO, HealthUnitCreateDTO, HealthUnitUpdateDTO>
    {
    }
}
