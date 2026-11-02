namespace mvp.Enums
{
    public enum RedistributionSuggestionStatus
    {
        /// <summary>Gerada pelo modelo, aguardando decisão humana.</summary>
        Pending,

        /// <summary>Aceita: o remanejamento foi executado (TransferOut + TransferIn).</summary>
        Accepted,

        /// <summary>Recusada por uma pessoa (motivo registrado — vira rótulo para avaliar o modelo).</summary>
        Rejected,

        /// <summary>Substituída por uma execução mais recente antes de ser decidida.</summary>
        Superseded
    }
}
