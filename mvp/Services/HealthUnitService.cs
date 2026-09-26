using mvp.DTOs;
using mvp.Entities;
using mvp.Exceptions;
using mvp.Interfaces.IRepositories;
using mvp.Interfaces.IServices;
using mvp.ValueObjects;

namespace mvp.Services
{
    public class HealthUnitService : IHealthUnitService
    {
        private readonly IHealthUnitRepository _healthUnitRepository;

        public HealthUnitService(IHealthUnitRepository healthUnitRepository)
        {
            _healthUnitRepository = healthUnitRepository;
        }

        public async Task<List<HealthUnitResponseDTO>> GetAllAsync()
        {
            var healthUnits = await _healthUnitRepository.GetAllAsync();

            return healthUnits.Select(hp => new HealthUnitResponseDTO
            {
                Id = hp.Id,
                Name = hp.Name,
                Street = hp.Address.Street,
                Number = hp.Address.Number,
                Neighborhood = hp.Address.Neighborhood,
                ZipCode = hp.Address.ZipCode,
                Zone = hp.Address.Zone,
                CNES= hp.CNES.Value,
            }).ToList();
        }

        public async Task<HealthUnitResponseDTO?> GetByIdAsync(Guid Id)
        {
            var healthUnit = await _healthUnitRepository.GetByIdAsync(Id);

            if (healthUnit == null)
                throw new HealthUnitNotFoundException();

            return new HealthUnitResponseDTO
            {
                Id = healthUnit.Id,
                Name = healthUnit.Name,
                Street = healthUnit.Address.Street,
                Number = healthUnit.Address.Number,
                Neighborhood = healthUnit.Address.Neighborhood,
                ZipCode = healthUnit.Address.ZipCode,
                Zone = healthUnit.Address.Zone,
                CNES = healthUnit.CNES.Value,   
            };
        }

        public async Task<HealthUnitResponseDTO> CreateAsync(HealthUnitCreateDTO dto)
        {
            var address = new Address(dto.Street, dto.Number, dto.Neighborhood, dto.ZipCode, dto.Zone);

            var cnes = new CNES(dto.CNES);

            var healthUnit = new HealthUnit(dto.Name, address, cnes);

            await _healthUnitRepository.CreateAsync(healthUnit);

            return new HealthUnitResponseDTO
            {
                Id = healthUnit.Id,
                Name = healthUnit.Name,
                Street = healthUnit.Address.Street,
                Number = healthUnit.Address.Number,
                Neighborhood = healthUnit.Address.Neighborhood,
                ZipCode = healthUnit.Address.ZipCode,
                Zone = healthUnit.Address.Zone,
                CNES = healthUnit.CNES.Value,
            };
        }

        public async Task<HealthUnitResponseDTO> UpdateAsync(Guid Id, HealthUnitUpdateDTO dto)
        {
            var healthUnit = await _healthUnitRepository.GetByIdAsync(Id);

            if (healthUnit == null)
                throw new HealthUnitNotFoundException();

            var address = new Address(dto.Street, dto.Number, dto.Neighborhood, dto.ZipCode, dto.Zone);

            var cnes = new CNES(dto.CNES);

            healthUnit.Update(dto.Name, address, cnes);

            await _healthUnitRepository.UpdateAsync(healthUnit);

            return new HealthUnitResponseDTO
            {
                Id = healthUnit.Id,
                Name = healthUnit.Name,
                Street = healthUnit.Address.Street,
                Number = healthUnit.Address.Number,
                Neighborhood = healthUnit.Address.Neighborhood,
                ZipCode = healthUnit.Address.ZipCode,
                Zone = healthUnit.Address.Zone,
                CNES= healthUnit.CNES.Value,
            };
        }

        public async Task DeleteAsync(Guid Id)
        {
            var healthUnit = await _healthUnitRepository.GetByIdAsync(Id);

            if (healthUnit == null)
                throw new HealthUnitNotFoundException();

            await _healthUnitRepository.DeleteAsync(healthUnit);
        }
    }
}
