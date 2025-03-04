
using Microsoft.EntityFrameworkCore;
using TaskCraft.DataBase;

using TaskCraft.Repositories;



#region Builder
var builder = WebApplication.CreateBuilder();


builder.Services.AddAutoMapper(typeof(Program));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ProjectRepository>();
builder.Services.AddScoped<ChatRepository>();
builder.Services.AddScoped<MessageRepository>();
builder.Services.AddScoped<TaskRepository>();
#endregion

#region Apps
var app = builder.Build();
#endregion


app.Run();
