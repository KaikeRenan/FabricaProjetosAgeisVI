using mvp.ValueObjects;

namespace mvp.Entities
{
    public class User : BaseEntity
    {
        public Email Email { get; private set; }
        public Password Password { get; private set; }

        protected User()
        {
            Email = null!;
            Password = null!;
        }

        public User(Email email, Password password)
        {
            this.Email = email;
            this.Password = password;
        }

        public void Update(Email email, Password password)
        {
            this.Email = email;
            this.Password = password;

            UpdateTimestamps();
        }
    }
}
