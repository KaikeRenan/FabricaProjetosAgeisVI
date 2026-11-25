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

        public StockMovementService(IStockMovementRepository stockMovementRepository)
        {
            _stockMovementRepository = stockMovementRepository;
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
    }
}
