using mvp.DTOs;
using mvp.Entities;
using mvp.Exceptions;
using mvp.Interfaces.IRepositories;
using mvp.Interfaces.IServices;
using mvp.ValueObjects;

namespace mvp.Services
{
    public class HealthPostService : IHealthPostService
    {
        private readonly IHealthPostRepository _healthPostRepository;

        public HealthPostService(IHealthPostRepository healthPostRepository)
        {
            _healthPostRepository = healthPostRepository;
        }

        public async Task<List<HealthPostResponseDTO>> GetAllAsync()
        {
            var healthPosts = await _healthPostRepository.GetAllAsync();

            return healthPosts.Select(hp => new HealthPostResponseDTO
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

        public async Task<HealthPostResponseDTO?> GetByIdAsync(Guid Id)
        {
            var healthPost = await _healthPostRepository.GetByIdAsync(Id);

            if (healthPost == null)
                throw new HealthPostNotFoundException();

            return new HealthPostResponseDTO
            {
                Id = healthPost.Id,
                Name = healthPost.Name,
                Street = healthPost.Address.Street,
                Number = healthPost.Address.Number,
                Neighborhood = healthPost.Address.Neighborhood,
                ZipCode = healthPost.Address.ZipCode,
                Zone = healthPost.Address.Zone,
                CNES = healthPost.CNES.Value,   
            };
        }

        public async Task<HealthPostResponseDTO> CreateAsync(HealthPostCreateDTO dto)
        {
            var address = new Address(dto.Street, dto.Number, dto.Neighborhood, dto.ZipCode, dto.Zone);

            var cnes = new CNES(dto.CNES);

            var healthPost = new HealthPost(dto.Name, address, cnes);

            await _healthPostRepository.CreateAsync(healthPost);

            return new HealthPostResponseDTO
            {
                Id = healthPost.Id,
                Name = healthPost.Name,
                Street = healthPost.Address.Street,
                Number = healthPost.Address.Number,
                Neighborhood = healthPost.Address.Neighborhood,
                ZipCode = healthPost.Address.ZipCode,
                Zone = healthPost.Address.Zone,
                CNES = healthPost.CNES.Value,
            };
        }

        public async Task<HealthPostResponseDTO> UpdateAsync(Guid Id, HealthPostUpdateDTO dto)
        {
            var healthPost = await _healthPostRepository.GetByIdAsync(Id);

            if (healthPost == null)
                throw new HealthPostNotFoundException();

            var address = new Address(dto.Street, dto.Number, dto.Neighborhood, dto.ZipCode, dto.Zone);

            var cnes = new CNES(dto.CNES);

            healthPost.Update(dto.Name, address, cnes);

            await _healthPostRepository.UpdateAsync(healthPost);

            return new HealthPostResponseDTO
            {
                Id = healthPost.Id,
                Name = healthPost.Name,
                Street = healthPost.Address.Street,
                Number = healthPost.Address.Number,
                Neighborhood = healthPost.Address.Neighborhood,
                ZipCode = healthPost.Address.ZipCode,
                Zone = healthPost.Address.Zone,
                CNES= healthPost.CNES.Value,
            };
        }

        public async Task DeleteAsync(Guid Id)
        {
            var healthPost = await _healthPostRepository.GetByIdAsync(Id);

            if (healthPost == null)
                throw new HealthPostNotFoundException();

            await _healthPostRepository.DeleteAsync(healthPost);
        }
    }
}
