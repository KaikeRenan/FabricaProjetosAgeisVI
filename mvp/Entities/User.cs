using mvp.Enums;
using mvp.ValueObjects;

namespace mvp.Entities
{
    public class User : BaseEntity
    {
        public Email Email { get; private set; }
        public Password Password { get; private set; }
        public UserRole Role { get; private set; }
        public CPF? CPF { get; private set; }

        protected User()
        {
            Email = null!;
            Password = null!;
        }

        public User(Email email, Password password, UserRole role, CPF? cpf)
        {
            this.Email = email;
            this.Password = password;
            Role = role;
            CPF = cpf;
        }

        public void Update(Email email, Password password, UserRole role, CPF? cpf)
        {
            this.Email = email;
            this.Password = password;
            Role = role;
            CPF = cpf;

            UpdateTimestamps();
        }
    }
}
