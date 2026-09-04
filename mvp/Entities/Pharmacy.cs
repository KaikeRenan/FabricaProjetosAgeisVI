using mvp.ValueObjects;

namespace mvp.Entities
{
    public class Pharmacy : BaseEntity
    {
        public Name name {get; private set; }
        public Address address { get; private set; }
        public CNPJ cnpj {get; private set; }
        public PhoneNumber phoneNumber {get; private set;}
        //Futuramente adicionar a chave estrangeira de Estoque

        protected Pharmacy()
        {
            name = null!;
            address = null!;
            cnpj = null;
            phoneNumber = null;
        }

        public Pharmacy(Name name, Address address, CNPJ cnpj, PhoneNumber phoneNumber)
        {
            this.name = name;
            this.address = address;
            this.cnpj = cnpj;
            this.phoneNumber = phoneNumber;
        }

        public void Update(Name name, Address address, CNPJ cnpj, PhoneNumber phoneNumber)
        {
            this.name = name;
            this.address = address;
            this.cnpj = cnpj;
            this.phoneNumber = phoneNumber;

            UpdateTimestamps();
        }
    }
}
