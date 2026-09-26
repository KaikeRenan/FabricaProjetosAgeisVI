using mvp.DTOs;

namespace mvp.Interfaces.IServices
{
    public interface IStockService : IBaseService<StockResponseDTO, StockCreateDTO, StockUpdateDTO>
    {
    }
}
