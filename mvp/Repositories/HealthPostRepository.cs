using mvp.Data;
using mvp.Entities;
using mvp.Interfaces.IRepositories;

namespace mvp.Repositories
{
    public class HealthPostRepository : BaseRepository<HealthPost>, IHealthPostRepository
    {
        public HealthPostRepository(Context context) : base(context)
        {
        }
    }
}
