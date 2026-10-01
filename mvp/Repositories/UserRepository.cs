using Microsoft.EntityFrameworkCore;
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
            Email emailObj;

            try
            {
                emailObj = new Email(email);
            }
            catch (ArgumentException)
            {
                return null;
            }

            //var normalized = email.Trim().ToLowerInvariant();

            return await _dbSet.FirstOrDefaultAsync(x => x.Email == emailObj && x.RemovedAt == null);
        }
    }
}
