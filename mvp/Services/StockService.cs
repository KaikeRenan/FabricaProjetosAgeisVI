using mvp.DTOs.Stock;
using mvp.Entities;
using mvp.Exceptions;
using mvp.Interfaces.IRepositories;
using mvp.Interfaces.IServices;
using System.Collections;

namespace mvp.Services
{
    public class StockService : IStockService
    {
        private readonly IStockRepository _stockRepository;

        public StockService(IStockRepository stockRepository)
        {
            _stockRepository = stockRepository;
        }

        private StockResponseDTO Response(Stock stock)
        {
            return new StockResponseDTO
            {
                Id = stock.Id,
                PharmacyId = stock.PharmacyId,
                HealthUnitId = stock.HealthUnitId,
                BatchId = stock.BatchId,
                Quantity = stock.Quantity,
            };
        }

        public async Task<List<StockResponseDTO>> GetAllAsync()
        {
            var stocks = await _stockRepository.GetAllAsync();

            return stocks.Select(Response).ToList();
        }

        public async Task<StockResponseDTO> GetByIdAsync(Guid Id)
        {
            var stock = await _stockRepository.GetByIdAsync(Id);

            if (stock == null)
                throw new StockNotFoundException();

            return Response(stock);
        }

        public async Task<StockResponseDTO> CreateAsync(StockCreateDTO dto)
        {
            var stock = new Stock(dto.PharmacyId, dto.HealthUnitId, dto.BatchId, dto.Quantity);

            await _stockRepository.CreateAsync(stock);

            return Response(stock);
        }

        public async Task<StockResponseDTO> UpdateAsync(Guid Id, StockUpdateDTO dto)
        {
            var stock = await _stockRepository.GetByIdAsync(Id);

            if (stock == null)
                throw new StockNotFoundException();

            stock.Update(dto.PharmacyId, dto.HealthUnitId, dto.BatchId, dto.Quantity);

            await _stockRepository.UpdateAsync(stock);

            return Response(stock);
        }

        public async Task DeleteAsync(Guid Id)
        {
            var stock = await _stockRepository.GetByIdAsync(Id);

            if (stock == null)
                throw new StockNotFoundException();

            await _stockRepository.DeleteAsync(stock);
        }
    }
}
