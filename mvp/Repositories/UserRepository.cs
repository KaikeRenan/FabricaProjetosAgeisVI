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
    }
}
