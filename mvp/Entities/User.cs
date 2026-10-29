using mvp.ValueObjects;

namespace mvp.Entities
{
    public class User : BaseEntity
    {
        public Email Email { get; private set; }
        public Password Password { get; private set; }
        public CPF Cpf {  get; private set; }

        protected User()
        {
            Email = null!;
            Password = null!;
            Cpf = null;
        }

        public User(Email email, Password password, CPF cpf)
        {
            this.Email = email;
            this.Password = password;
            this.Cpf = cpf;
        }

        public void Update(Email email, Password password, CPF cpf)
        {
            this.Email = email;
            this.Password = password;
            this.Cpf = cpf;

            UpdateTimestamps();
        }
    }
}
