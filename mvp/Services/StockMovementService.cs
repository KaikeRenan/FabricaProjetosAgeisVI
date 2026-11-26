using mvp.DTOs.Common;
using mvp.DTOs.StockMovement;
using mvp.Entities;
using mvp.Exceptions;
using mvp.Interfaces.IRepositories;
using mvp.Interfaces.IServices;

namespace mvp.Services
{
    public class StockMovementService : IStockMovementService
    {
        private readonly IStockMovementRepository _stockMovementRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IBatchRepository _batchRepository;
        private readonly IMedicineRepository _medicineRepository;

        public StockMovementService(
            IStockMovementRepository stockMovementRepository,
            IStockRepository stockRepository,
            IBatchRepository batchRepository,
            IMedicineRepository medicineRepository)
        {
            _stockMovementRepository = stockMovementRepository;
            _stockRepository = stockRepository;
            _batchRepository = batchRepository;
            _medicineRepository = medicineRepository;
        }

        private StockMovementResponseDTO Response(StockMovement stockMovement)
        {
            return new StockMovementResponseDTO
            {
                Id = stockMovement.Id,
                StockId = stockMovement.StockId,
                BatchId = stockMovement.BatchId,
                MedicineId = stockMovement.MedicineId,
                Type = stockMovement.Type,
                Quantity = stockMovement.Quantity,
                OccurredAt = stockMovement.OccurredAt,
                TransferId = stockMovement.TransferId,
            };
        }

        public async Task<List<StockMovementResponseDTO>> GetAllAsync()
        {
            var stockMovements = await _stockMovementRepository.GetAllAsync();

            return stockMovements.Select(Response).ToList();
        }

        public async Task<StockMovementResponseDTO?> GetByIdAsync(Guid id)
        {
            var stockMovement = await _stockMovementRepository.GetByIdAsync(id);

            if (stockMovement == null)
                throw new StockMovementNotFoundException();

            return Response(stockMovement);
        }

        public async Task<StockMovementResponseDTO> CreateAsync(StockMovementCreateDTO dto)
        {
            await EnsureReferencesAsync(dto.StockId, dto.BatchId, dto.MedicineId);

            var stockMovement = new StockMovement(
                dto.StockId,
                dto.BatchId,
                dto.MedicineId,
                dto.Type,
                dto.Quantity,
                dto.OccurredAt,
                dto.TransferId
            );

            await _stockMovementRepository.CreateWithStockUpdateAsync(stockMovement);

            return Response(stockMovement);
        }

        public async Task<StockMovementResponseDTO> UpdateAsync(Guid id, StockMovementUpdateDTO dto)
        {
            await EnsureReferencesAsync(dto.StockId, dto.BatchId, dto.MedicineId);

            var stockMovement = await _stockMovementRepository.GetByIdAsync(id);

            if (stockMovement == null)
                throw new StockMovementNotFoundException();

            stockMovement.Update(
                dto.StockId,
                dto.BatchId,
                dto.MedicineId,
                dto.Type,
                dto.Quantity
            );

            await _stockMovementRepository.UpdateAsync(stockMovement);

            return Response(stockMovement);
        }

        public async Task DeleteAsync(Guid id)
        {
            var stockMovement = await _stockMovementRepository.GetByIdAsync(id);

            if (stockMovement == null)
                throw new StockMovementNotFoundException();

            await _stockMovementRepository.DeleteAsync(stockMovement);
        }

        private const int MaxPageSize = 200;

        public async Task<PagedResult<StockMovementResponseDTO>> GetPagedAsync(int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var (items, total) = await _stockMovementRepository.GetPagedAsync((page - 1) * pageSize, pageSize);

            return new PagedResult<StockMovementResponseDTO>
            {
                Items = items.Select(Response).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
        }

        private async Task EnsureReferencesAsync(Guid stockId, Guid batchId, Guid medicineId)
        {
            if (await _stockRepository.GetByIdAsync(stockId) is null)
                throw new StockNotFoundException();

            if (await _batchRepository.GetByIdAsync(batchId) is null)
                throw new BatchNotFoundException();

            if (await _medicineRepository.GetByIdAsync(medicineId) is null)
                throw new MedicineNotFoundException();
        }
    }
}
