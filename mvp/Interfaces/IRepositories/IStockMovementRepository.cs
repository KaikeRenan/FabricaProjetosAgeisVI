using mvp.Entities;
using mvp.Enums;

namespace mvp.Interfaces.IRepositories
{
    public interface IStockMovementRepository : IBaseRepository<StockMovement>
    {
        Task<(List<StockMovement> Items, int Total)> GetPagedAsync(int skip, int take);
        Task CreateWithStockUpdateAsync(StockMovement movement);
    }
}
