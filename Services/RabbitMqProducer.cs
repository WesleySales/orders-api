using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace OrdersApi.Services;

public interface IRabbitMqProducer
{
    Task PublicarEventoAsync<T>(string queueName, T evento);
}

public class RabbitMqProducer : IRabbitMqProducer
{
    private readonly IConfiguration _configuration;

    public RabbitMqProducer(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task PublicarEventoAsync<T>(string queueName, T evento)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost"
        };

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        var json = JsonSerializer.Serialize(evento);
        var body = Encoding.UTF8.GetBytes(json);

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            body: body
        );
    }
}