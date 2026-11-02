using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using mvp.Clients.Contracts;
using mvp.Exceptions;

namespace mvp.Clients
{
    /// <summary>
    /// Cliente tipado (HttpClientFactory) do serviço de ML. Toda falha de rede, timeout, status != 2xx ou resposta
    /// fora do contrato vira MlServiceUnavailableException (HTTP 503): o chamador nunca recebe detalhes internos.
    /// </summary>
    public class MlServiceClient : IMlServiceClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient _http;
        private readonly ILogger<MlServiceClient> _logger;

        public MlServiceClient(HttpClient http, ILogger<MlServiceClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<MlRecommendationResponse> GetRecommendationsAsync(MlRecommendationRequest request, CancellationToken cancellationToken)
        {
            return await SendAsync<MlRecommendationResponse>(
                () => _http.PostAsJsonAsync("v1/recommendations", request, JsonOptions, cancellationToken),
                cancellationToken);
        }

        public async Task<MlModelInfo> GetModelInfoAsync(CancellationToken cancellationToken)
        {
            return await SendAsync<MlModelInfo>(
                () => _http.GetAsync("v1/model", cancellationToken),
                cancellationToken);
        }

        private async Task<T> SendAsync<T>(Func<Task<HttpResponseMessage>> send, CancellationToken cancellationToken)
        {
            if (_http.BaseAddress is null)
                throw new MlServiceUnavailableException("Serviço de ML não configurado (MlService:BaseUrl/ApiKey)");

            try
            {
                using var response = await send();

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Serviço de ML respondeu {Status}: {Body}", (int)response.StatusCode, body);
                    throw new MlServiceUnavailableException($"Serviço de ML indisponível ou recusou a requisição (HTTP {(int)response.StatusCode})");
                }

                var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
                return result ?? throw new MlServiceUnavailableException("Resposta vazia do serviço de ML");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Falha de rede ao chamar o serviço de ML");
                throw new MlServiceUnavailableException("Não foi possível contatar o serviço de ML");
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Timeout ao chamar o serviço de ML");
                throw new MlServiceUnavailableException("Tempo esgotado ao aguardar o serviço de ML");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Resposta do serviço de ML fora do contrato v1");
                throw new MlServiceUnavailableException("Resposta do serviço de ML fora do contrato esperado");
            }
        }
    }
}
