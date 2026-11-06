using mvp.DTOs.Redistribution;
using mvp.Enums;

namespace mvp.Interfaces.IServices
{
    public interface IRedistributionService
    {
        Task<RedistributionRunResponseDTO> GenerateRunAsync(RedistributionRunRequestDTO dto, string? triggeredBy, CancellationToken cancellationToken);
        Task<RedistributionRunResponseDTO> GetRunAsync(Guid id);
        Task<List<RedistributionSuggestionResponseDTO>> GetSuggestionsAsync(Guid? runId, RedistributionSuggestionStatus? status, int take);
        Task<RedistributionSuggestionResponseDTO> AcceptAsync(Guid id, string decidedBy);
        Task<RedistributionSuggestionResponseDTO> RejectAsync(Guid id, RejectSuggestionDTO dto, string decidedBy);
        Task<MlModelInfoResponseDTO> GetModelInfoAsync(CancellationToken cancellationToken);
    }
}
