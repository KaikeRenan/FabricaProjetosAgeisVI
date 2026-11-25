using Microsoft.EntityFrameworkCore;
using mvp.Data;
using mvp.Entities;
using mvp.Exceptions;
using mvp.Interfaces.IRepositories;
using mvp.Enums;

namespace mvp.Repositories
{
    public class StockMovementRepository : BaseRepository<StockMovement>, IStockMovementRepository
    {
        private readonly Context _context;

        public StockMovementRepository(Context context) : base(context) 
        {
            _context = context;
        }

        public async Task CreateWithStockUpdateAsync(StockMovement movement)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            var now = DateTime.UtcNow;
            var delta = movement.Type is StockMovementType.Entry or StockMovementType.TransferIn
                ? movement.Quantity
                : -movement.Quantity;

            var stocks = _context.Stocks.Where(s => s.Id == movement.StockId && s.RemovedAt == null);

            int rows;
            if (delta >= 0)
                rows = await stocks.ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Quantity, x => x.Quantity + delta)
                    .SetProperty(x => x.UpdatedAt, now));
            else
                rows = await stocks.Where(x => x.Quantity >= -delta).ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Quantity, x => x.Quantity - (-delta))
                    .SetProperty(x => x.UpdatedAt, now));

            if (rows == 0)
            {
                var exists = await stocks.AnyAsync();
                throw exists ? new InsufficientStockException() : new StockNotFoundException();
            }

            await _dbSet.AddAsync(movement);
            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();
        }

        public async Task<(List<StockMovement> Items, int Total)> GetPagedAsync(int skip, int take)
        {
            var query = _dbSet.AsNoTracking().Where(m => m.RemovedAt == null);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(m => m.OccurredAt)
                .ThenByDescending(m => m.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (items, total);
        }
    }
}
