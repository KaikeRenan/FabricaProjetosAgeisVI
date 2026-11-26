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
        private readonly IMedicineRepository _medicineRepository;
        private readonly IBatchRepository _batchRepository;

        public ExpirationAlertService(
            IExpirationAlertRepository expirationAlertRepository,
            IMedicineRepository medicineRepository,
            IBatchRepository batchRepository)
        {
            _expirationAlertRepository = expirationAlertRepository;
            _medicineRepository = medicineRepository;
            _batchRepository = batchRepository;
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
            await EnsureReferencesAsync(dto.MedicineId, dto.BatchId);

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
            await EnsureReferencesAsync(dto.MedicineId, dto.BatchId);

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

        public Task<List<ExpirationAlertResponseDTO>> GetAllWithDetailsAsync() => _expirationAlertRepository.GetAllWithDetailsAsync();

        public async Task<ExpirationAlertResponseDTO?> GetByIdWithDetailsAsync(Guid id)
        {
            var alert = await _expirationAlertRepository.GetByIdWithDetailsAsync(id);

            if (alert is null)
                throw new ExpirationAlertNotFoundException();

            return alert;
        }

        public async Task<ExpirationAlertGenerationResultDTO> GenerateAsync(int daysAhead, bool autoResolve, CancellationToken ct) 
        {
            if (daysAhead is < 0 or > 365)
                throw new ArgumentException("daysAhead deve estar entre 0 e 365.", nameof(daysAhead));

            return await _expirationAlertRepository.GenerateAsync(daysAhead, autoResolve, ct);
        }

        private async Task EnsureReferencesAsync(Guid medicineId, Guid batchId)
        {
            if (await _medicineRepository.GetByIdAsync(medicineId) is null)
                throw new MedicineNotFoundException();

            if (await _batchRepository.GetByIdAsync(batchId) is null)
                throw new BatchNotFoundException();
        }
    }
}
