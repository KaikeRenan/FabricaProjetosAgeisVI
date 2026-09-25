namespace mvp.DTOs
{
    public class HealthUnitResponseDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Street { get; set; } = null!;
        public string Number { get; set; } = null!;
        public string Neighborhood { get; set; } = null!;
        public string ZipCode { get; set; } = null!;
        public string Zone { get; set; } = null!;
        public string CNES { get; set; } = null!;
    }
}
