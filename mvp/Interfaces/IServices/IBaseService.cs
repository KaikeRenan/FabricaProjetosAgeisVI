namespace mvp.Interfaces.IServices
{
    public interface IBaseService<TResponse, TCreate, TUpdate>
    {
        Task<List<TResponse>> GetAllAsync();
        Task<TResponse?> GetByIdAsync(Guid id);
        Task<TResponse> CreateAsync(TCreate dto);
        Task<TResponse> UpdateAsync(Guid id, TUpdate dto);
        Task DeleteAsync(Guid id);
    }
}
