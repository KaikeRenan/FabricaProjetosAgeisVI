using mvp.DTOs;

namespace mvp.Interfaces.IServices
{
    public interface IStockService
    {
        Task<List<StockResponseDTO>> GetAllAsync();
        Task<StockResponseDTO?> GetByIdAsync(Guid Id);
        Task<StockResponseDTO> CreateAsync(StockCreateDTO dto);
        Task<StockResponseDTO> UpdateAsync(Guid Id, StockUpdateDTO dto);
        Task DeleteAsync(Guid Id);
    }
}
