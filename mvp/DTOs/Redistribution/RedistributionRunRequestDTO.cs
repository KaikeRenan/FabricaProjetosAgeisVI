using System.ComponentModel.DataAnnotations;

namespace mvp.DTOs.Redistribution
{
    /// <summary>Todos os campos são opcionais: o serviço de ML aplica os padrões quando omitidos.</summary>
    public class RedistributionRunRequestDTO
    {
        public DateOnly? AsOf { get; set; }

        [Range(0, 30)]
        public int? LeadTimeDays { get; set; }

        [Range(0, 365)]
        public int? MinShelfLifeDays { get; set; }

        [Range(1, 100000)]
        public int? MinTransferQty { get; set; }

        /// <summary>&gt; 0 deixa o doador mais conservador (menos excedente é considerado transferível).</summary>
        [Range(0, 3)]
        public double? DonorDemandZ { get; set; }

        [Range(0, 3)]
        public double? ReceiverDemandZ { get; set; }

        [Range(0.1, 100000)]
        public double? MaxDistanceKm { get; set; }
    }
}
