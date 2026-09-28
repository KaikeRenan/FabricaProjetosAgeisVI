using mvp.DTOs.HealthUnit;

namespace mvp.Interfaces.IServices
{
    public interface IHealthUnitService : IBaseService<HealthUnitResponseDTO, HealthUnitCreateDTO, HealthUnitUpdateDTO>
    {
    }
}
