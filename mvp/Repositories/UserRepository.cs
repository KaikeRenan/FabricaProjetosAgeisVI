using mvp.Data;
using mvp.Entities;
using mvp.Interfaces.IRepositories;
using mvp.ValueObjects;

namespace mvp.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(Context context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            // Compare the converted property directly so EF Core can translate the
            // value object to the underlying "email" column.
            return _dbSet.FirstOrDefault(x => x.Email == new Email(email));
        }
    }
}
