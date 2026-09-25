using mvp.Data;
using mvp.Entities;
using mvp.Interfaces.IRepositories;

namespace mvp.Repositories
{
    public class HealthUnitRepository : BaseRepository<HealthUnit>, IHealthUnitRepository
    {
        public HealthUnitRepository(Context context) : base(context)
        {
        }
    }
}
