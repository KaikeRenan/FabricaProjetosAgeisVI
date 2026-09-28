using mvp.DTOs.User;
using mvp.Entities;
using mvp.Exceptions;
using mvp.Interfaces.IRepositories;
using mvp.Interfaces.IServices;
using mvp.ValueObjects;

namespace mvp.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        private UserResponseDTO Response(User user)
        {
            return new UserResponseDTO
            {
                Id = user.Id,
                Email = user.Email.Value
            };
        }

        public async Task<List<UserResponseDTO>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();

            return users.Select(Response).ToList();
        }

        public async Task<UserResponseDTO> GetByIdAsync(Guid Id)
        {
            var user = await _userRepository.GetByIdAsync(Id);

            if (user == null)
                throw new UserNotFoundException();

            return Response(user);
        }

        public async Task<UserResponseDTO> CreateAsync(UserCreateDTO dto)
        {
            var email = new Email(dto.Email);
            var password = new Password(dto.Password);

            var user = new User(email, password);

            await _userRepository.CreateAsync(user);

            return Response(user);
        }

        public async Task<UserResponseDTO> UpdateAsync(Guid Id, UserUpdateDTO dto)
        {
            var user = await _userRepository.GetByIdAsync(Id);

            if (user == null)
                throw new UserNotFoundException();

            var email = new Email(dto.Email);
            var password = new Password(dto.Password);

            user.Update(email, password);

            await _userRepository.UpdateAsync(user);

            return Response(user);
        }

        public async Task DeleteAsync(Guid Id)
        {
            var user = await _userRepository.GetByIdAsync(Id);

            if (user == null)
                throw new UserNotFoundException();

            await _userRepository.DeleteAsync(user);
        }

        public async Task<UserResponseDTO> GetByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
                throw new UserNotFoundException();

            return Response(user);
        }
    }
}
