using mvp.Data;
using mvp.Entities;
using mvp.Interfaces.IRepositories;

namespace mvp.Repositories
{
    public class ExpirationAlertRepository : BaseRepository<ExpirationAlert>, IExpirationAlertRepository
    {
        public ExpirationAlertRepository(Context context) : base(context)
        {
        }
    }
}
