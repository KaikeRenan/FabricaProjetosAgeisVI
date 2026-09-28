using mvp.DTOs.User;
using mvp.Entities;

namespace mvp.Interfaces.IServices
{
    public interface IUserService : IBaseService<UserResponseDTO, UserCreateDTO, UserUpdateDTO>
    {
        Task<UserResponseDTO> GetByEmailAsync(string email);
    }
}
