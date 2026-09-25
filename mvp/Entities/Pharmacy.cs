using mvp.ValueObjects;

namespace mvp.Entities
{
    public class Pharmacy : BaseEntity
    {
        public string Name { get; private set; }
        public Address Address { get; private set; }
        public CNPJ CNPJ { get; private set; }
        public PhoneNumber PhoneNumber { get; private set; }
        //Futuramente adicionar a chave estrangeira de Estoque

        protected Pharmacy()
        {
            Name = null!;
            Address = null!;
            CNPJ = null!;
            PhoneNumber = null!;
        }

        public Pharmacy(string name, Address address, CNPJ cnpj, PhoneNumber phoneNumber)
        {
            this.Name = name;
            this.Address = address;
            this.CNPJ = cnpj;
            this.PhoneNumber = phoneNumber;
        }

        public void Update(string name, Address address, CNPJ cnpj, PhoneNumber phoneNumber)
        {
            this.Name = name;
            this.Address = address;
            this.CNPJ = cnpj;
            this.PhoneNumber = phoneNumber;

            UpdateTimestamps();
        }
    }
}
