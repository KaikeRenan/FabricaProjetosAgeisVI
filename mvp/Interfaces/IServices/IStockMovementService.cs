using mvp.DTOs.StockMovement;

namespace mvp.Interfaces.IServices
{
    public interface IStockMovementService : IBaseService<StockMovementResponseDTO, StockMovementCreateDTO, StockMovementUpdateDTO>
    {
    }
}
