using mvp.Config;
using mvp.DTOs.User;
using mvp.Enums;
using mvp.Exceptions;
using mvp.Interfaces.IServices;

namespace mvp.Data
{
    /// Garante os usuários de bootstrap no startup (cria só se faltar) e pelo menos um Admin ativo.
    public static class UserBootstrap
    {
        public static async Task EnsureAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
        {
            var settings = configuration.GetSection("BootstrapUsers").Get<BootstrapUsersSettings>()
                           ?? new BootstrapUsersSettings();

            if (!settings.Enabled)
                return;

            if (settings.Users.Count == 0)
                throw new InvalidOperationException("BootstrapUsers habilitado, mas nenhum usuário foi configurado.");

            if (!settings.Users.Any(u => u.Role == UserRole.Admin))
                throw new InvalidOperationException("BootstrapUsers precisa conter ao menos um usuário com Role=Admin.");

            using var scope = services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<IUserService>();

            foreach (var u in settings.Users)
            {
                if (string.IsNullOrWhiteSpace(u.Email) || string.IsNullOrWhiteSpace(u.Password))
                    throw new InvalidOperationException("BootstrapUsers: Email e Password são obrigatórios.");

                try
                {
                    await users.GetByEmailAsync(u.Email);   // já existe: não mexe
                    continue;
                }
                catch (UserNotFoundException) { }

                await users.CreateAsync(new UserCreateDTO
                {
                    Email = u.Email,
                    Password = u.Password,
                    CPF = u.CPF,
                    Role = u.Role
                });
                logger.LogWarning("BootstrapUsers: {Role} {Email} criado.", u.Role, u.Email);
            }

            // invariante: sempre existe ao menos um Admin ativo
            var active = await users.GetAllAsync();
            if (active.Any(u => u.Role == UserRole.Admin))
                return;

            var admin = settings.Users.First(u => u.Role == UserRole.Admin);
            var existing = await users.GetByEmailAsync(admin.Email!);
            await users.UpdateAsync(existing.Id, new UserUpdateDTO
            {
                Email = admin.Email!,
                Password = admin.Password!,
                CPF = existing.CPF,
                Role = UserRole.Admin
            });
            logger.LogWarning("BootstrapUsers: {Email} promovido a Admin.", admin.Email);
        }
    }
}
