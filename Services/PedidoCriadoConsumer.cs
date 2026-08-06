namespace OrdersApi.Services;

using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class PedidoCriadoConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PedidoCriadoConsumer> _logger;

    public PedidoCriadoConsumer(IConfiguration configuration, ILogger<PedidoCriadoConsumer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost"
        };

        using var connection = await factory.CreateConnectionAsync(stoppingToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        string queueName = "PedidoCriado";

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var jsonMessage = Encoding.UTF8.GetString(body);

            _logger.LogInformation("1. PEDIDO CRIADO: {Json}\n      2. O email foi enviado para o cliente\n     3. O Estoque do produto foi atualizado", jsonMessage);

            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}