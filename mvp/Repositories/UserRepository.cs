using mvp.Data;
using mvp.Entities;
using mvp.Interfaces.IRepositories;

namespace mvp.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(Context context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return _dbSet.FirstOrDefault(x => x.Email.Value == email);
        }
    }
}
