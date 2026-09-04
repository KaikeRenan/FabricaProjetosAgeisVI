using Microsoft.EntityFrameworkCore;
using mvp.Data;
using mvp.Interfaces.IRepositories;
using mvp.Interfaces.IServices;
using mvp.Repositories;
using mvp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IHealthPostService, HealthPostService>();
builder.Services.AddScoped<IHealthPostRepository, HealthPostRepository>();

//builder.Services.AddDbContext<Context>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
