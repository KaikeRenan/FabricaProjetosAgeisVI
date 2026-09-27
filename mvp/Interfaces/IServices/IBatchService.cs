using mvp.DTOs.Batch;

namespace mvp.Interfaces.IServices
{
    public interface IBatchService : IBaseService<BatchResponseDTO, BatchCreateDTO, BatchUpdateDTO>
    {
    }
}
