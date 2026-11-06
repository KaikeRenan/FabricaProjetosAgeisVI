using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using mvp.Clients;
using mvp.Config;
using mvp.Data;
using mvp.Interfaces.IRepositories;
using mvp.Interfaces.IServices;
using mvp.Interfaces.Services;
using mvp.Middleware;
using mvp.Repositories;
using mvp.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtSettings>()!;

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt")
);

builder.Services.AddTransient<TokenService>();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddControllers();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IHealthUnitService, HealthUnitService>();
builder.Services.AddScoped<IHealthUnitRepository, HealthUnitRepository>();

builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IStockRepository, StockRepository>();

builder.Services.AddScoped<IMedicineService, MedicineService>();
builder.Services.AddScoped<IMedicineRepository, MedicineRepository>();

builder.Services.AddScoped<IStockMovementService, StockMovementService>();
builder.Services.AddScoped<IStockMovementRepository, StockMovementRepository>();

builder.Services.AddScoped<IBatchService, BatchService>();
builder.Services.AddScoped<IBatchRepository, BatchRepository>();

builder.Services.AddScoped<IExpirationAlertService, ExpirationAlertService>();
builder.Services.AddScoped<IExpirationAlertRepository, ExpirationAlertRepository>();

builder.Services.AddScoped<IRedistributionService, RedistributionService>();
builder.Services.AddScoped<IRedistributionRepository, RedistributionRepository>();

// Serviço de ML (previsão de demanda + recomendação de remanejamento). Opcional: se MlService não estiver
// configurado, a API sobe normalmente e apenas os endpoints /api/redistribution respondem 503.
builder.Services.Configure<MlServiceSettings>(builder.Configuration.GetSection("MlService"));
builder.Services.AddHttpClient<IMlServiceClient, MlServiceClient>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<MlServiceSettings>>().Value;

    if (string.IsNullOrWhiteSpace(settings.BaseUrl) || string.IsNullOrWhiteSpace(settings.ApiKey))
        return;

    client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    client.DefaultRequestHeaders.Add("X-Api-Key", settings.ApiKey);
});

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.ASCII.GetBytes(jwtSettings.PrivateKey)
            ),

            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddDbContext<Context>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
