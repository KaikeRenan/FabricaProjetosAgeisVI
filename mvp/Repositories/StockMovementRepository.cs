using mvp.Data;
using mvp.Entities;
using mvp.Interfaces.IRepositories;

namespace mvp.Repositories
{
    public class StockMovementRepository : BaseRepository<StockMovement>, IStockMovementRepository
    {
        public StockMovementRepository(Context context) : base(context)
        {
        }
    }
}
