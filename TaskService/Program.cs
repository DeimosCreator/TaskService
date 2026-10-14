using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Endpoints; // Подключаем пространство имен с нашими эндпоинтами
using TaskApi.Services;
using TaskApi.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// сервисы
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<ITaskEventPublisher, RabbitMqPublisher>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// конвейер
app.UseSwagger();
app.UseSwaggerUI();

// миграции
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate(); 
}

app.MapTaskEndpoints();

app.Run();

public partial class Program { }