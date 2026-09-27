using mvp.Data;
using mvp.Entities;
using mvp.Interfaces.IRepositories;

namespace mvp.Repositories
{
    public class BatchRepository : BaseRepository<Batch>, IBatchRepository
    {
        public BatchRepository(Context context) : base(context)
        {
        }
    }
}
