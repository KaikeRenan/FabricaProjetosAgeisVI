namespace mvp.Config
{
    public class MlServiceSettings
    {
        /// <summary>URL base do serviço de ML (ex.: http://localhost:8000/).</summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>Segredo compartilhado enviado no header X-Api-Key. Use variável de ambiente/secret manager.</summary>
        public string ApiKey { get; set; } = string.Empty;

        public int TimeoutSeconds { get; set; } = 60;
    }
}
