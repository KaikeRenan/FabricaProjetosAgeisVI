using mvp.Entities;
using mvp.Enums;

namespace mvp.Interfaces.IRepositories
{
    public enum DecisionOutcome
    {
        Applied,
        NotFound,
        InvalidState,
        InsufficientStock
    }

    /// <summary>
    /// Repositório do agregado de remanejamento. Não herda IBaseRepository: não é CRUD — as operações de decisão
    /// precisam ser atômicas e tocar Stock/StockMovement na mesma transação.
    /// </summary>
    public interface IRedistributionRepository
    {
        /// <summary>Grava a execução e as sugestões; opcionalmente marca como Superseded as pendentes anteriores.</summary>
        Task AddRunAsync(RedistributionRun run, IReadOnlyCollection<RedistributionSuggestion> suggestions, bool supersedePending);

        Task<RedistributionRun?> GetRunAsync(Guid id);

        Task<List<RedistributionSuggestion>> GetSuggestionsAsync(Guid? runId, RedistributionSuggestionStatus? status, int take);

        Task<RedistributionSuggestion?> GetSuggestionAsync(Guid id);

        /// <summary>Pending -> Rejected de forma atômica (compare-and-set).</summary>
        Task<DecisionOutcome> TryRejectAsync(Guid id, string decidedBy, string reason);

        /// <summary>
        /// Pending -> Accepted + baixa na origem + entrada no destino + dois StockMovements
        /// (TransferOut/TransferIn), tudo em UMA transação. Qualquer falha desfaz tudo.
        /// </summary>
        Task<DecisionOutcome> AcceptAndTransferAsync(Guid id, string decidedBy);
    }
}
