using mvp.DTOs.User;
using mvp.Entities;
using mvp.Enums;
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
                Email = user.Email.Value,
                CPF = user.CPF?.Value,
                Role = user.Role
            };
        }

        public async Task<List<UserResponseDTO>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();

            return users.Select(Response).ToList();
        }

        public async Task<UserResponseDTO?> GetByIdAsync(Guid Id)
        {
            var user = await _userRepository.GetByIdAsync(Id);

            if (user == null)
                throw new UserNotFoundException();

            return Response(user);
        }

        public async Task<UserResponseDTO> CreateAsync(UserCreateDTO dto)
        {
            if (await _userRepository.GetByEmailAsync(dto.Email) is not null)
                throw new EmailAlreadyRegisteredException();

            if (!string.IsNullOrWhiteSpace(dto.CPF) && await _userRepository.GetByCPFAsync(dto.CPF) is not null)
                throw new CPFAlreadyRegisteredException();

            var email = new Email(dto.Email);
            var password = new Password(dto.Password);
            var cpf = string.IsNullOrWhiteSpace(dto.CPF) ? null : new CPF(dto.CPF);

            var user = new User(email, password, dto.Role ?? UserRole.Operator, cpf);

            await _userRepository.CreateAsync(user);

            return Response(user);
        }

        public async Task<UserResponseDTO> UpdateAsync(Guid Id, UserUpdateDTO dto)
        {
            var user = await _userRepository.GetByIdAsync(Id);

            if (user == null)
                throw new UserNotFoundException();

            var sameEmail = await _userRepository.GetByEmailAsync(dto.Email);
            if (sameEmail is not null && sameEmail.Id != user.Id)
                throw new EmailAlreadyRegisteredException();

            var email = new Email(dto.Email);
            var password = new Password(dto.Password);

            var cpf = dto.CPF is null
                ? user.CPF                                  // não enviado: mantém
                : string.IsNullOrWhiteSpace(dto.CPF)
                    ? null                                  // string vazia: remove
                    : new CPF(dto.CPF);

            if (cpf is not null && cpf.Value != user.CPF?.Value)
            {
                var existing = await _userRepository.GetByCPFAsync(cpf.Value);
                if (existing is not null && existing.Id != user.Id)
                    throw new CPFAlreadyRegisteredException();
            }

            user.Update(email, password, dto.Role ?? user.Role, cpf);

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
