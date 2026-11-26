using mvp.DTOs.Stock;
using mvp.Entities;
using mvp.Exceptions;
using mvp.Interfaces.IRepositories;
using mvp.Interfaces.IServices;

namespace mvp.Services
{
    public class StockService : IStockService
    {
        private readonly IStockRepository _stockRepository;
        private readonly IBatchRepository _batchRepository;
        private readonly IHealthUnitRepository _healthUnitRepository;
        private readonly IPharmacyRepository _pharmacyRepository;

        public StockService(
            IStockRepository stockRepository,
            IBatchRepository batchRepository,
            IHealthUnitRepository healthUnitRepository,
            IPharmacyRepository pharmacyRepository)
        {
            _stockRepository = stockRepository;
            _batchRepository = batchRepository;
            _healthUnitRepository = healthUnitRepository;
            _pharmacyRepository = pharmacyRepository;
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

        public async Task<StockResponseDTO?> GetByIdAsync(Guid Id)
        {
            var stock = await _stockRepository.GetByIdAsync(Id);

            if (stock == null)
                throw new StockNotFoundException();

            return Response(stock);
        }

        public async Task<StockResponseDTO> CreateAsync(StockCreateDTO dto)
        {
            await EnsureReferencesAsync(dto.PharmacyId, dto.HealthUnitId, dto.BatchId);

            var stock = new Stock(dto.PharmacyId, dto.HealthUnitId, dto.BatchId, dto.Quantity);

            await _stockRepository.CreateAsync(stock);

            return Response(stock);
        }

        public async Task<StockResponseDTO> UpdateAsync(Guid Id, StockUpdateDTO dto)
        {
            await EnsureReferencesAsync(dto.PharmacyId, dto.HealthUnitId, dto.BatchId);

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

        private async Task EnsureReferencesAsync(Guid? pharmacyId, Guid? healthUnitId, Guid? batchId)
        {
            if (batchId is not null && await _batchRepository.GetByIdAsync(batchId.Value) is null)
                throw new BatchNotFoundException();

            if (healthUnitId is not null && await _healthUnitRepository.GetByIdAsync(healthUnitId.Value) is null)
                throw new HealthUnitNotFoundException();

            if (pharmacyId is not null && await _pharmacyRepository.GetByIdAsync(pharmacyId.Value) is null)
                throw new PharmacyNotFoundException();
        }
    }
}
