using mvp.DTOs.ExpirationAlert;
using mvp.Entities;
using mvp.Exceptions;
using mvp.Interfaces.IRepositories;
using mvp.Interfaces.IServices;

namespace mvp.Services
{
    public class ExpirationAlertService : IExpirationAlertService
    {
        private readonly IExpirationAlertRepository _expirationAlertRepository;

        public ExpirationAlertService(IExpirationAlertRepository expirationAlertRepository)
        {
            _expirationAlertRepository = expirationAlertRepository;
        }

        private ExpirationAlertResponseDTO Response(ExpirationAlert expirationAlert)
        {
            return new ExpirationAlertResponseDTO
            {
                Id = expirationAlert.Id,
                MedicineId = expirationAlert.MedicineId,
                BatchId = expirationAlert.BatchId,
                AlertDate = expirationAlert.AlertDate,
                Resolved = expirationAlert.Resolved,
            };
        }

        public async Task<List<ExpirationAlertResponseDTO>> GetAllAsync()
        {
            var expirationAlerts = await _expirationAlertRepository.GetAllAsync();

            return expirationAlerts.Select(Response).ToList();
        }

        public async Task<ExpirationAlertResponseDTO?> GetByIdAsync(Guid Id)
        {
            var expirationAlert = await _expirationAlertRepository.GetByIdAsync(Id);

            if (expirationAlert == null)
                throw new ExpirationAlertNotFoundException();

            return Response(expirationAlert);
        }

        public async Task<ExpirationAlertResponseDTO> CreateAsync(ExpirationAlertCreateDTO dto)
        {
            var expirationAlert = new ExpirationAlert(
                dto.MedicineId,
                dto.BatchId,
                dto.AlertDate
            );

            await _expirationAlertRepository.CreateAsync(expirationAlert);

            return Response(expirationAlert);
        }

        public async Task<ExpirationAlertResponseDTO> UpdateAsync(Guid Id, ExpirationAlertUpdateDTO dto)
        {
            var expirationAlert = await _expirationAlertRepository.GetByIdAsync(Id);

            if (expirationAlert == null)
                throw new ExpirationAlertNotFoundException();

            expirationAlert.Update(
                dto.MedicineId,
                dto.BatchId,
                dto.AlertDate,
                dto.Resolved
            );

            await _expirationAlertRepository.UpdateAsync(expirationAlert);

            return Response(expirationAlert);
        }

        public async Task DeleteAsync(Guid Id)
        {
            var expirationAlert = await _expirationAlertRepository.GetByIdAsync(Id);

            if (expirationAlert == null)
                throw new ExpirationAlertNotFoundException();

            await _expirationAlertRepository.DeleteAsync(expirationAlert);
        }
    }
}
