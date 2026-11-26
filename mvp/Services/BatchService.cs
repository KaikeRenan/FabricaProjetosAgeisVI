using mvp.DTOs.Batch;
using mvp.Entities;
using mvp.Exceptions;
using mvp.Interfaces.IRepositories;
using mvp.Interfaces.IServices;

namespace mvp.Services
{
    public class BatchService : IBatchService
    {
        private readonly IBatchRepository _batchRepository;
        private readonly IMedicineRepository _medicineRepository;

        public BatchService(IBatchRepository batchRepository, IMedicineRepository medicineRepository)
        {
            _batchRepository = batchRepository;
            _medicineRepository = medicineRepository;
        }

        private BatchResponseDTO Response(Batch batch)
        {
            return new BatchResponseDTO
            {
                Id = batch.Id,
                MedicineId = batch.MedicineId,
                BatchNumber = batch.BatchNumber,
                FabricationDate = batch.FabricationDate,
                ExpirationDate = batch.ExpirationDate,
            };
        }

        public async Task<List<BatchResponseDTO>> GetAllAsync()
        {
            var batches = await _batchRepository.GetAllAsync();

            return batches.Select(Response).ToList();
        }

        public async Task<BatchResponseDTO?> GetByIdAsync(Guid Id)
        {
            var batch = await _batchRepository.GetByIdAsync(Id);

            if (batch == null)
                throw new BatchNotFoundException();

            return Response(batch);
        }

        public async Task<BatchResponseDTO> CreateAsync(BatchCreateDTO dto)
        {
            await EnsureMedicineExistsAsync(dto.MedicineId);

            var batch = new Batch(
                dto.MedicineId, 
                dto.BatchNumber, 
                dto.FabricationDate,
                dto.ExpirationDate
            );

            await _batchRepository.CreateAsync(batch);

            return Response(batch);
        }

        public async Task<BatchResponseDTO> UpdateAsync(Guid Id, BatchUpdateDTO dto)
        {
            await EnsureMedicineExistsAsync(dto.MedicineId);

            var batch = await _batchRepository.GetByIdAsync(Id);

            if (batch == null)
                throw new BatchNotFoundException();

            batch.Update(
                dto.MedicineId,
                dto.BatchNumber,
                dto.FabricationDate,
                dto.ExpirationDate
            );

            await _batchRepository.UpdateAsync(batch);

            return Response(batch);
        }

        public async Task DeleteAsync(Guid Id)
        {
            var batch = await _batchRepository.GetByIdAsync(Id);

            if (batch == null)
                throw new BatchNotFoundException();

            await _batchRepository.DeleteAsync(batch);
        }

        private async Task EnsureMedicineExistsAsync(Guid medicineId)
        {
            if (await _medicineRepository.GetByIdAsync(medicineId) is null)
                throw new MedicineNotFoundException();
        }
    }
}
