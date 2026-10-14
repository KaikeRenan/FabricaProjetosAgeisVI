using mvp.DTOs.StockMovement;
using mvp.Entities;
using mvp.Enums;
using mvp.Exceptions;
using mvp.Interfaces.IRepositories;
using mvp.Interfaces.IServices;

namespace mvp.Services
{
    public class StockMovementService : IStockMovementService
    {
        private readonly IStockMovementRepository _stockMovementRepository;
        private readonly IStockRepository _stockRepository;

        public StockMovementService(IStockMovementRepository stockMovementRepository, IStockRepository stockRepository)
        {
            _stockMovementRepository = stockMovementRepository;
            _stockRepository = stockRepository;
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
                TransferId = stockMovement.TransferId
            };
        }

        private void ApplyMovement(Stock stock, StockMovementType type, int quantity)
        {
            if (type == StockMovementType.Exit)
            {
                if (stock.Quantity < quantity)
                    throw new Exception("Quantidade insuficiente em estoque.");

                stock.DecreaseQuantity(quantity);
            }
            else if (type == StockMovementType.Entry)
            {
                stock.IncreaseQuantity(quantity);
            }
        }

        private void RevertMovement(Stock stock, StockMovementType type, int quantity)
        {
            if (type == StockMovementType.Exit)
            {
                stock.IncreaseQuantity(quantity);
            }
            else if (type == StockMovementType.Entry)
            {
                if (stock.Quantity < quantity)
                    throw new Exception("Não é possível desfazer: o estoque ficaria negativo.");

                stock.DecreaseQuantity(quantity);
            }
        }

        public async Task<List<StockMovementResponseDTO>> GetAllAsync()
        {
            var stockMovements = await _stockMovementRepository.GetAllAsync();

            return stockMovements.Select(Response).ToList();
        }

        public async Task<StockMovementResponseDTO> GetByIdAsync(Guid id)
        {
            var stockMovement = await _stockMovementRepository.GetByIdAsync(id);

            if (stockMovement == null)
                throw new StockMovementNotFoundException();

            return Response(stockMovement);
        }

        public async Task<StockMovementResponseDTO> CreateAsync(StockMovementCreateDTO dto)
        {
            var stock = await _stockRepository.GetByIdAsync(dto.StockId);

            if (stock == null)
                throw new Exception("Estoque não encontrado.");

            ApplyMovement(stock, dto.Type, dto.Quantity);

            var stockMovement = new StockMovement(
                dto.StockId,
                dto.BatchId,
                dto.MedicineId,
                dto.Type,
                dto.Quantity,
                dto.OccurredAt ?? DateTime.UtcNow,
                dto.TransferId
            );

            await _stockMovementRepository.CreateAsync(stockMovement);
            await _stockRepository.UpdateAsync(stock);

            return Response(stockMovement);
        }

        public async Task<StockMovementResponseDTO> UpdateAsync(Guid id, StockMovementUpdateDTO dto)
        {
            var stockMovement = await _stockMovementRepository.GetByIdAsync(id);

            if (stockMovement == null)
                throw new StockMovementNotFoundException();

            var oldStock = await _stockRepository.GetByIdAsync(stockMovement.StockId);

            if (oldStock == null)
                throw new Exception("Estoque não encontrado.");

            RevertMovement(oldStock, stockMovement.Type, stockMovement.Quantity);

            var newStock = dto.StockId == oldStock.Id
                ? oldStock
                : await _stockRepository.GetByIdAsync(dto.StockId);

            if (newStock == null)
                throw new Exception("Estoque não encontrado.");

            ApplyMovement(newStock, dto.Type, dto.Quantity);

            stockMovement.Update(
                dto.StockId,
                dto.BatchId,
                dto.MedicineId,
                dto.Type,
                dto.Quantity,
                dto.OccurredAt ?? stockMovement.OccurredAt,
                dto.TransferId
            );

            await _stockMovementRepository.UpdateAsync(stockMovement);
            await _stockRepository.UpdateAsync(oldStock);

            if (newStock.Id != oldStock.Id)
                await _stockRepository.UpdateAsync(newStock);

            return Response(stockMovement);
        }

        public async Task DeleteAsync(Guid id)
        {
            var stockMovement = await _stockMovementRepository.GetByIdAsync(id);

            if (stockMovement == null)
                throw new StockMovementNotFoundException();

            var stock = await _stockRepository.GetByIdAsync(stockMovement.StockId);

            if (stock == null)
                throw new Exception("Estoque não encontrado.");

            RevertMovement(stock, stockMovement.Type, stockMovement.Quantity);

            await _stockMovementRepository.DeleteAsync(stockMovement);
            await _stockRepository.UpdateAsync(stock);
        }
    }
}
