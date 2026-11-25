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
            return await _dbSet.FirstOrDefaultAsync(x => x.Email == new Email(email) && x.RemovedAt == null);
        }

        public async Task<User?> GetByCPFAsync(string cpf)
        {
            var value = new CPF(cpf);
            return await _dbSet.FirstOrDefaultAsync(x => x.CPF == value && x.RemovedAt == null);
        }
    }
}
