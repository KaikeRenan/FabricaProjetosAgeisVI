using mvp.Data;
using mvp.Entities;
using mvp.Interfaces.IRepositories;

namespace mvp.Repositories
{
    public class MedicineRepository : BaseRepository<Medicine>, IMedicineRepository
    {
        public MedicineRepository(Context context) : base(context)
        {
        }
    }
}
