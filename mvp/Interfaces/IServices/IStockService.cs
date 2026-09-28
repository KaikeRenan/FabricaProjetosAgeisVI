using mvp.DTOs.Stock;

namespace mvp.Interfaces.IServices
{
    public interface IStockService : IBaseService<StockResponseDTO, StockCreateDTO, StockUpdateDTO>
    {
    }
}
