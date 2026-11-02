namespace mvp.Enums
{
    /// <summary>
    /// Persistido como TEXTO em stock_movements.type (HasConversion&lt;string&gt;), portanto acrescentar membros
    /// NÃO exige migração. Não reordene nem renomeie membros existentes: os valores gravados são os nomes.
    /// </summary>
    public enum StockMovementType
    {
        Entry,

        /// <summary>
        /// Saída por dispensação/consumo. É o ÚNICO tipo tratado como DEMANDA pelo modelo de previsão.
        /// </summary>
        Exit,

        /// <summary>Entrada por remanejamento entre unidades (não é compra).</summary>
        TransferIn,

        /// <summary>Saída por remanejamento entre unidades (NÃO é consumo).</summary>
        TransferOut,

        /// <summary>Perda: vencimento, avaria ou descarte (NÃO é consumo).</summary>
        Loss
    }
}
