using mvp.Data;
using mvp.Entities;
using mvp.Interfaces.IRepositories;

namespace mvp.Repositories
{
    public class PharmacyRepository : BaseRepository<Pharmacy>, IPharmacyRepository
    {
        public PharmacyRepository(Context context) : base(context)
        {
        }
    }
}
