using mvp.DTOs.Medicine;

namespace mvp.Interfaces.IServices
{
    public interface IMedicineService : IBaseService<MedicineResponseDTO, MedicineCreateDTO, MedicineUpdateDTO>
    {
    }
}
