using mvp.DTOs;
using mvp.Entities;

namespace mvp.Interfaces.IServices
{
    public interface IUserService
    {
        Task<List<UserResponseDTO>> GetAllAsync();
        Task<UserResponseDTO> GetByIdAsync(Guid Id);
        Task<UserResponseDTO> CreateAsync(UserCreateDTO dto);
        Task<UserResponseDTO> UpdateAsync(Guid Id, UserUpdateDTO dto);
        Task DeleteAsync(Guid Id);

        Task<UserResponseDTO> GetByEmailAsync(string email);
    }
}
