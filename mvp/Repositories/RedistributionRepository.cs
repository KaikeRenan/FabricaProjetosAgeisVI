using Microsoft.EntityFrameworkCore;
using mvp.Data;
using mvp.Entities;
using mvp.Enums;
using mvp.Interfaces.IRepositories;

namespace mvp.Repositories
{
    public class RedistributionRepository : IRedistributionRepository
    {
        private readonly Context _context;

        public RedistributionRepository(Context context)
        {
            _context = context;
        }

        public async Task AddRunAsync(RedistributionRun run, IReadOnlyCollection<RedistributionSuggestion> suggestions, bool supersedePending)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            if (supersedePending)
            {
                var now = DateTime.UtcNow;

                await _context.RedistributionSuggestions
                    .Where(s => s.Status == RedistributionSuggestionStatus.Pending && s.RemovedAt == null)
                    .ExecuteUpdateAsync(set => set
                        .SetProperty(s => s.Status, RedistributionSuggestionStatus.Superseded)
                        .SetProperty(s => s.UpdatedAt, now));
            }

            await _context.RedistributionRuns.AddAsync(run);
            await _context.RedistributionSuggestions.AddRangeAsync(suggestions);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        public async Task<RedistributionRun?> GetRunAsync(Guid id)
        {
            return await _context.RedistributionRuns
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id && r.RemovedAt == null);
        }

        public async Task<List<RedistributionSuggestion>> GetSuggestionsAsync(Guid? runId, RedistributionSuggestionStatus? status, int take)
        {
            var query = _context.RedistributionSuggestions
                .AsNoTracking()
                .Where(s => s.RemovedAt == null);

            if (runId.HasValue)
                query = query.Where(s => s.RunId == runId.Value);

            if (status.HasValue)
                query = query.Where(s => s.Status == status.Value);

            return await query
                .OrderBy(s => s.DaysToExpiry)
                .ThenByDescending(s => s.ProjectedWasteAtSource)
                .Take(take)
                .ToListAsync();
        }

        public async Task<RedistributionSuggestion?> GetSuggestionAsync(Guid id)
        {
            return await _context.RedistributionSuggestions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && s.RemovedAt == null);
        }

        public async Task<DecisionOutcome> TryRejectAsync(Guid id, string decidedBy, string reason)
        {
            var now = DateTime.UtcNow;

            var rows = await _context.RedistributionSuggestions
                .Where(s => s.Id == id && s.Status == RedistributionSuggestionStatus.Pending && s.RemovedAt == null)
                .ExecuteUpdateAsync(set => set
                    .SetProperty(s => s.Status, RedistributionSuggestionStatus.Rejected)
                    .SetProperty(s => s.DecidedAt, (DateTime?)now)
                    .SetProperty(s => s.DecidedBy, decidedBy)
                    .SetProperty(s => s.RejectionReason, reason)
                    .SetProperty(s => s.UpdatedAt, now));

            if (rows == 1)
                return DecisionOutcome.Applied;

            return await ExistsAsync(id) ? DecisionOutcome.InvalidState : DecisionOutcome.NotFound;
        }

        public async Task<DecisionOutcome> AcceptAndTransferAsync(Guid id, string decidedBy)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var now = DateTime.UtcNow;

            // 1) "Claim" atômico: só uma requisição concorrente consegue mudar Pending -> Accepted.
            var claimed = await _context.RedistributionSuggestions
                .Where(s => s.Id == id && s.Status == RedistributionSuggestionStatus.Pending && s.RemovedAt == null)
                .ExecuteUpdateAsync(set => set
                    .SetProperty(s => s.Status, RedistributionSuggestionStatus.Accepted)
                    .SetProperty(s => s.DecidedAt, (DateTime?)now)
                    .SetProperty(s => s.DecidedBy, decidedBy)
                    .SetProperty(s => s.UpdatedAt, now));

            if (claimed == 0)
                return await ExistsAsync(id) ? DecisionOutcome.InvalidState : DecisionOutcome.NotFound;

            var suggestion = await _context.RedistributionSuggestions
                .AsNoTracking()
                .FirstAsync(s => s.Id == id);

            var quantity = suggestion.Quantity;

            // 2) Baixa atômica na origem (UPDATE ... WHERE quantity >= q): sem "lost update" com dispensações concorrentes.
            var origin = await FindStockAsync(suggestion.FromLocationKind, suggestion.FromLocationId, suggestion.BatchId);
            if (origin is null)
                return DecisionOutcome.InsufficientStock; // o dispose da transação desfaz o "claim"

            var decremented = await _context.Stocks
                .Where(s => s.Id == origin.Id && s.Quantity >= quantity)
                .ExecuteUpdateAsync(set => set
                    .SetProperty(s => s.Quantity, s => s.Quantity - quantity)
                    .SetProperty(s => s.UpdatedAt, now));

            if (decremented == 0)
                return DecisionOutcome.InsufficientStock;

            // 3) Entrada no destino (cria a linha de estoque do lote naquele local se ainda não existir).
            var destination = await FindStockAsync(suggestion.ToLocationKind, suggestion.ToLocationId, suggestion.BatchId);
            Guid destinationStockId;

            if (destination is null)
            {
                var created = suggestion.ToLocationKind == LocationKind.Pharmacy
                    ? new Stock(suggestion.ToLocationId, null, suggestion.BatchId, quantity)
                    : new Stock(null, suggestion.ToLocationId, suggestion.BatchId, quantity);

                await _context.Stocks.AddAsync(created);
                destinationStockId = created.Id;
            }
            else
            {
                await _context.Stocks
                    .Where(s => s.Id == destination.Id)
                    .ExecuteUpdateAsync(set => set
                        .SetProperty(s => s.Quantity, s => s.Quantity + quantity)
                        .SetProperty(s => s.UpdatedAt, now));
                destinationStockId = destination.Id;
            }

            // 4) Trilha de auditoria: TransferOut/TransferIn NÃO são consumo (o modelo de demanda os ignora).
            await _context.StockMovements.AddRangeAsync(
                new StockMovement(origin.Id, suggestion.BatchId, suggestion.MedicineId, StockMovementType.TransferOut, quantity),
                new StockMovement(destinationStockId, suggestion.BatchId, suggestion.MedicineId, StockMovementType.TransferIn, quantity));

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return DecisionOutcome.Applied;
        }

        private async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.RedistributionSuggestions.AnyAsync(s => s.Id == id && s.RemovedAt == null);
        }

        private async Task<Stock?> FindStockAsync(LocationKind kind, Guid locationId, Guid batchId)
        {
            var query = _context.Stocks
                .AsNoTracking()
                .Where(s => s.RemovedAt == null && s.BatchId == batchId);

            query = kind == LocationKind.Pharmacy
                ? query.Where(s => s.PharmacyId == locationId)
                : query.Where(s => s.HealthUnitId == locationId);

            return await query.OrderByDescending(s => s.Quantity).FirstOrDefaultAsync();
        }
    }
}
