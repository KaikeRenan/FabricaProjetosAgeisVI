using mvp.Entities;

namespace mvp.Interfaces.IRepositories
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
    }
}
