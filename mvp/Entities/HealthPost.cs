using mvp.ValueObjects;

namespace mvp.Entities
{
    public class HealthPost : BaseEntity
    {
        public string Name { get; private set; }
        public Address Address { get; private set; }
        public CNES CNES { get; private set; }

        protected HealthPost()
        {
            Name = null!;
            Address = null!;
            CNES = null!;
        }

        public HealthPost(string name, Address address, CNES cnes)
        {
            this.Name = name;
            this.Address = address;
            this.CNES = cnes;
        }

        public void Update(string name, Address address, CNES cnes)
        {
            this.Name = name;
            this.Address = address;
            this.CNES = cnes;
            UpdateTimestamps();
        }
    }
}
