using mvp.DTOs.Common;
using mvp.DTOs.StockMovement;

namespace mvp.Interfaces.IServices
{
    public interface IStockMovementService : IBaseService<StockMovementResponseDTO, StockMovementCreateDTO, StockMovementUpdateDTO>
    {
        Task<PagedResult<StockMovementResponseDTO>> GetPagedAsync(int page, int pageSize);
    }
}
