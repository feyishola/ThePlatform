using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PlatformService.Dtos;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PlatformService.AsyncDataServices
{
    public class MessageBusClient : IMessageBusClient
    {
        private readonly IConfiguration _configuration;
        private readonly IConnection? _connection;
        private readonly IChannel? _channel;
        public MessageBusClient(IConfiguration configuration)
        {
            _configuration = configuration;
            var rabbitMqHost = _configuration["RabbitMQHost"];
            var rabbitMqPort = _configuration["RabbitMQPort"];
            
            Console.WriteLine($"---> Initializing MessageBusClient");
            Console.WriteLine($"---> RabbitMQHost: {rabbitMqHost ?? "NOT CONFIGURED"}");
            Console.WriteLine($"---> RabbitMQPort: {rabbitMqPort ?? "NOT CONFIGURED"}");
            
            if (string.IsNullOrEmpty(rabbitMqHost) || string.IsNullOrEmpty(rabbitMqPort))
            {
                Console.WriteLine("--> ERROR: RabbitMQ configuration is missing!");
                throw new ArgumentException("RabbitMQ configuration is missing. Please check RabbitMQHost and RabbitMQPort settings.");
            }
            
            var factory = new ConnectionFactory()
            {
                HostName = rabbitMqHost,
                Port = int.Parse(rabbitMqPort)
            };

            try
            {
                Console.WriteLine($"---> Attempting to connect to RabbitMQ at {rabbitMqHost}:{rabbitMqPort}");
                //Since this code is in a constructor (which cannot be marked as async), we can't use the await keyword. Using .GetAwaiter().GetResult() allows us to synchronously wait for async operations to complete, which is appropriate in this initialization context.
                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                Console.WriteLine($"---> Connection established: {_connection.IsOpen}");
                
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
                Console.WriteLine($"---> Channel created: {_channel.IsOpen}");

                _channel.ExchangeDeclareAsync(exchange: "trigger", type: ExchangeType.Fanout).GetAwaiter().GetResult();
                Console.WriteLine($"---> Exchange 'trigger' declared");

                _connection.ConnectionShutdownAsync += RabbitMQ_ConnectionShutdown;

                Console.WriteLine("--> Connected to MessageBus");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> CRITICAL ERROR: Could not connect to the Message Bus");
                Console.WriteLine($"---> Error: {ex.Message}");
                Console.WriteLine($"---> Type: {ex.GetType().Name}");
                Console.WriteLine($"---> Inner Exception: {ex.InnerException?.Message}");
                Console.WriteLine($"---> Connection and Channel will be NULL");
            }
        }
        public async Task PublishNewPlatformAsync(PlatformPublishedDto platformPublishedDto)
        {
            if(_channel == null)
            {
                Console.WriteLine("--> ERROR: MessageBus channel is not initialized. Cannot publish message.");
                Console.WriteLine("---> TIP: Check RabbitMQ connection during startup");
                return;
            }

            var message = JsonSerializer.Serialize(platformPublishedDto);
            Console.WriteLine($"---> Publishing event: {platformPublishedDto.Event}");

            if(_connection?.IsOpen == true)
            {
                Console.WriteLine("--> RabbitMQ Connection open, sending message...");
                await SendMessageAsync(message);
            }else
            {
                Console.WriteLine("--> RabbitMQ Connection is closed, not sending message...");
                Console.WriteLine($"---> Connection status: {_connection?.IsOpen}");
            }
        }

        private async Task SendMessageAsync(string message)
        {
            if(_channel == null)
            {
                Console.WriteLine("--> ERROR: Channel is null in SendMessageAsync");
                throw new InvalidOperationException("RabbitMQ channel is not initialized");
            }

            try
            {
                var body = Encoding.UTF8.GetBytes(message);
                Console.WriteLine($"---> Publishing message to exchange 'trigger'");

                var basicProperties = new BasicProperties();
                await _channel.BasicPublishAsync<BasicProperties>(exchange: "trigger", routingKey: "", mandatory: false, basicProperties: basicProperties, body: body);

                Console.WriteLine($"--> Successfully sent: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> ERROR in SendMessageAsync: {ex.Message}");
                Console.WriteLine($"---> Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public void Dispose()
        {
            Console.WriteLine("Message Bus Disposed");
            if(_channel?.IsOpen == true)
            {
                _channel.CloseAsync();
                _connection?.CloseAsync();
            }
        }

        private Task RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            Console.WriteLine("---> RabbitMQ Connection Shutdown");
            return Task.CompletedTask;
        }
    }
}