using mvp.DTOs;
using mvp.Entities;
using mvp.Exceptions;
using mvp.Interfaces.IRepositories;
using mvp.Interfaces.IServices;

namespace mvp.Services
{
    public class StockService : IStockService
    {
        private readonly IStockRepository _stockRepository;

        public StockService(IStockRepository stockRepository)
        {
            _stockRepository = stockRepository;
        }

        public async Task<List<StockResponseDTO>> GetAllAsync()
        {
            var stocks = await _stockRepository.GetAllAsync();

            return stocks.Select(s => new StockResponseDTO
            {
                Id = s.Id,
                PharmacyId = s.PharmacyId,
                HealthPostId = s.HealthPostId,
                MedicineName = s.MedicineName,
                Dosage = s.Dosage,
                Quantity = s.Quantity,
            }).ToList();
        }

        public async Task<StockResponseDTO?> GetByIdAsync(Guid Id)
        {
            var stock = await _stockRepository.GetByIdAsync(Id);

            if (stock == null)
                throw new StockNotFoundException();

            return new StockResponseDTO
            {
                Id = stock.Id,
                PharmacyId = stock.PharmacyId,
                HealthPostId = stock.HealthPostId,
                MedicineName = stock.MedicineName,
                Dosage = stock.Dosage,
                Quantity = stock.Quantity,
            };
        }

        public async Task<StockResponseDTO> CreateAsync(StockCreateDTO dto)
        {
            var stock = new Stock(dto.PharmacyId, dto.HealthPostId, dto.MedicineName, dto.Dosage, dto.Quantity);

            await _stockRepository.CreateAsync(stock);

            return new StockResponseDTO
            {
                Id = stock.Id,
                PharmacyId = stock.PharmacyId,
                HealthPostId = stock.HealthPostId,
                MedicineName = stock.MedicineName,
                Dosage = stock.Dosage,
                Quantity = stock.Quantity,
            };
        }

        public async Task<StockResponseDTO> UpdateAsync(Guid Id, StockUpdateDTO dto)
        {
            var stock = await _stockRepository.GetByIdAsync(Id);

            if (stock == null)
                throw new StockNotFoundException();

            stock.Update(dto.PharmacyId, dto.HealthPostId, dto.MedicineName, dto.Dosage, dto.Quantity);

            await _stockRepository.UpdateAsync(stock);

            return new StockResponseDTO
            {
                Id = stock.Id,
                PharmacyId = stock.PharmacyId,
                HealthPostId = stock.HealthPostId,
                MedicineName = stock.MedicineName,
                Dosage = stock.Dosage,
                Quantity = stock.Quantity,
            };
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
