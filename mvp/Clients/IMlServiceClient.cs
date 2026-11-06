using mvp.Clients.Contracts;

namespace mvp.Clients
{
    public interface IMlServiceClient
    {
        Task<MlRecommendationResponse> GetRecommendationsAsync(MlRecommendationRequest request, CancellationToken cancellationToken);
        Task<MlModelInfo> GetModelInfoAsync(CancellationToken cancellationToken);
    }
}
