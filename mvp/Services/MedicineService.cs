using mvp.DTOs.Medicine;
using mvp.Entities;
using mvp.Exceptions;
using mvp.Interfaces.IRepositories;
using mvp.Interfaces.IServices;

namespace mvp.Services
{
    public class MedicineService : IMedicineService
    {
        private readonly IMedicineRepository _medicineRepository;

        public MedicineService(IMedicineRepository medicineRepository)
        {
            _medicineRepository = medicineRepository;
        }

        private MedicineResponseDTO Response(Medicine medicine)
        {
            return new MedicineResponseDTO
            {
                Id = medicine.Id,
                Name = medicine.Name,
                ActiveIngredient = medicine.ActiveIngredient,
                Dosage = medicine.Dosage,
                Unit = medicine.Unit
            };
        }

        public async Task<List<MedicineResponseDTO>> GetAllAsync()
        {
            var medicines = await _medicineRepository.GetAllAsync();

            return medicines.Select(Response).ToList();
        }

        public async Task<MedicineResponseDTO?> GetByIdAsync(Guid id)
        {
            var medicine = await _medicineRepository.GetByIdAsync(id);

            if (medicine == null)
                return null;

            return Response(medicine);
        }

        public async Task<MedicineResponseDTO> CreateAsync(MedicineCreateDTO dto)
        {
            var medicine = new Medicine(
                dto.Name,
                dto.ActiveIngredient,
                dto.Dosage,
                dto.Unit
            );

            await _medicineRepository.CreateAsync(medicine);

            return Response(medicine);
        }

        public async Task<MedicineResponseDTO> UpdateAsync(Guid id, MedicineUpdateDTO dto)
        {
            var medicine = await _medicineRepository.GetByIdAsync(id);

            if (medicine == null)
                throw new MedicineNotFoundException();

            medicine.Update(
                dto.Name,
                dto.ActiveIngredient,
                dto.Dosage,
                dto.Unit
            );

            await _medicineRepository.UpdateAsync(medicine);

            return Response(medicine);
        }

        public async Task DeleteAsync(Guid id)
        {
            var medicine = await _medicineRepository.GetByIdAsync(id);

            if (medicine == null)
                throw new MedicineNotFoundException();

            await _medicineRepository.DeleteAsync(medicine);
        }
    }

}
