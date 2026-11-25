using Microsoft.EntityFrameworkCore;
using mvp.Data;
using mvp.DTOs.ExpirationAlert;
using mvp.Entities;
using mvp.Interfaces.IRepositories;

namespace mvp.Repositories
{
    public class ExpirationAlertRepository : BaseRepository<ExpirationAlert>, IExpirationAlertRepository
    {
        private readonly Context _context;

        public ExpirationAlertRepository(Context context) : base(context) 
        {
            _context = context;
        }

        public async Task<List<ExpirationAlertResponseDTO>> GetAllWithDetailsAsync()
        {
            var today = DateTime.UtcNow.Date;

            var items = await DetailedQuery()
                .OrderByDescending(a => a.AlertDate)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            foreach (var item in items)
                item.DaysToExpiry = (item.ExpirationDate.Date - today).Days;

            return items;
        }

        public async Task<ExpirationAlertResponseDTO?> GetByIdWithDetailsAsync(Guid id)
        {
            var item = await DetailedQuery().FirstOrDefaultAsync(a => a.Id == id);

            if (item is not null)
                item.DaysToExpiry = (item.ExpirationDate.Date - DateTime.UtcNow.Date).Days;

            return item;
        }

        private IQueryable<ExpirationAlertResponseDTO> DetailedQuery()
        {
            return
                from a in _context.ExpirationAlerts.AsNoTracking()
                join b in _context.Batches.AsNoTracking() on a.BatchId equals b.Id
                join m in _context.Medicines.AsNoTracking() on a.MedicineId equals m.Id
                where a.RemovedAt == null
                select new ExpirationAlertResponseDTO
                {
                    Id = a.Id,
                    MedicineId = a.MedicineId,
                    MedicineName = m.Name,
                    BatchId = a.BatchId,
                    BatchNumber = b.BatchNumber,
                    Quantity = _context.Stocks
                        .Where(s => s.BatchId == b.Id && s.RemovedAt == null)
                        .Sum(s => (int?)s.Quantity) ?? 0,
                    ExpirationDate = b.ExpirationDate,
                    AlertDate = a.AlertDate,
                    Resolved = a.Resolved
                };
        }

        public async Task<ExpirationAlertGenerationResultDTO> GenerateAsync(int daysAhead, bool autoResolve, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var horizon = now.Date.AddDays(Math.Max(0, daysAhead));

            var candidates = await (
                from s in _context.Stocks.AsNoTracking()
                join b in _context.Batches.AsNoTracking() on s.BatchId equals b.Id
                join m in _context.Medicines.AsNoTracking() on b.MedicineId equals m.Id
                where s.RemovedAt == null && s.Quantity > 0
                      && b.RemovedAt == null && m.RemovedAt == null
                      && b.ExpirationDate <= horizon
                select new { BatchId = b.Id, MedicineId = b.MedicineId })
                .Distinct()
                .ToListAsync(ct);

            var active = await _context.ExpirationAlerts
                .Where(a => a.RemovedAt == null && !a.Resolved)
                .Select(a => new { a.Id, a.BatchId })
                .ToListAsync(ct);

            var activeBatches = active.Select(a => a.BatchId).ToHashSet();

            var created = candidates
                .Where(c => !activeBatches.Contains(c.BatchId))
                .Select(c => new ExpirationAlert(c.MedicineId, c.BatchId, now))
                .ToList();

            var result = new ExpirationAlertGenerationResultDTO
            {
                Scanned = candidates.Count,
                Created = created.Count
            };

            if (created.Count > 0)
            {
                await _dbSet.AddRangeAsync(created, ct);
                await _dbContext.SaveChangesAsync(ct);
            }

            if (autoResolve)
            {
                var stocked = (await _context.Stocks.AsNoTracking()
                        .Where(s => s.RemovedAt == null && s.Quantity > 0 && s.BatchId != null)
                        .Select(s => s.BatchId!.Value)
                        .Distinct()
                        .ToListAsync(ct))
                    .ToHashSet();

                var toResolve = active
                    .Where(a => !stocked.Contains(a.BatchId))
                    .Select(a => a.Id)
                    .ToList();

                if (toResolve.Count > 0)
                    result.Resolved = await _context.ExpirationAlerts
                        .Where(a => toResolve.Contains(a.Id))
                        .ExecuteUpdateAsync(set => set
                            .SetProperty(a => a.Resolved, true)
                            .SetProperty(a => a.UpdatedAt, now), ct);
            }

            return result;
        }
    }
}
