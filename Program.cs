using Microsoft.EntityFrameworkCore;
using OrdersApi.Data;
using OrdersApi.Services;
using System.Reflection;
using OrdersApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Configuração do Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

// Configuração da conexão com o banco
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseSqlServer(connectionString));

// Configuração do RabbitMq

// producer -> envia as msgs pro rabbit
builder.Services.AddScoped<IRabbitMqProducer, RabbitMqProducer>();

// consumers -> consulta as msgs das filas de pedido criado e cancelado
builder.Services.AddHostedService<PedidoCriadoConsumer>();
builder.Services.AddHostedService<PedidoCanceladoConsumer>();

// Configuração do GlobalExceptionHandler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseExceptionHandler();

app.MapControllers();

app.Run();