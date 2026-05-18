using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandsService.EventProcessing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CommandsService.AsyncDataServices
{
    public class MessageBusSubscriber : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IEventProcessor _eventProcessor;
        private IConnection? _connection;
        private IChannel? _channel;
        private string? _queueName;

        public MessageBusSubscriber(IConfiguration configuration, IEventProcessor eventProcessor)
        {
            _configuration = configuration;
            _eventProcessor = eventProcessor;
        }
        
        private async Task InitializeRabbitMQ()
        {
            var rabbitMqHost = _configuration["RabbitMQHost"];
            var rabbitMqPort = _configuration["RabbitMQPort"];
            
            if (string.IsNullOrEmpty(rabbitMqHost) || string.IsNullOrEmpty(rabbitMqPort))
            {
                throw new ArgumentException("RabbitMQ configuration is missing. Please check RabbitMQHost and RabbitMQPort settings.");
            }

            var factory = new ConnectionFactory()
            {
                HostName = rabbitMqHost,
                Port = int.Parse(rabbitMqPort),
                UserName = "guest",
                Password = "guest"
            };

            try
            {
                Console.WriteLine("--> Creating RabbitMQ connection...");
                _connection = await factory.CreateConnectionAsync();
                Console.WriteLine($"--> RabbitMQ connection created. IsOpen: {_connection.IsOpen}");
                
                _channel = await _connection.CreateChannelAsync();
                Console.WriteLine($"--> RabbitMQ channel created. IsOpen: {_channel.IsOpen}");

                Console.WriteLine("--> Declaring exchange 'trigger'...");
                await _channel.ExchangeDeclareAsync(exchange: "trigger", type: ExchangeType.Fanout);
                Console.WriteLine("--> Exchange declared successfully");

                Console.WriteLine("--> Declaring queue...");
                var queueDeclareResult = await _channel.QueueDeclareAsync();
                _queueName = queueDeclareResult.QueueName;
                Console.WriteLine($"--> Queue declared: {_queueName}");

                Console.WriteLine($"--> Binding queue '{_queueName}' to exchange 'trigger'...");
                await _channel.QueueBindAsync(queue: _queueName, exchange: "trigger", routingKey: "");
                Console.WriteLine("--> Queue bound successfully");
                
                // Verify the queue exists and get message count
                var queueInfo = await _channel.QueueDeclarePassiveAsync(_queueName);
                Console.WriteLine($"--> Queue is passive declared. Message count: {queueInfo.MessageCount}, Consumer count: {queueInfo.ConsumerCount}");

                Console.WriteLine("--> Listening on the MessageBus");

                _connection.ConnectionShutdownAsync += RabbitMQ_ConnectionShutdown;
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"--> Could not connect to the Message Bus: {ex.Message}");
            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("--> MessageBusSubscriber ExecuteAsync started");
            stoppingToken.ThrowIfCancellationRequested();

            // Initialize RabbitMQ connection
            await InitializeRabbitMQ();

            if (_channel == null || _queueName == null)
            {
                Console.WriteLine("--> Channel not initialized, cannot start consuming messages");
                return;
            }

            Console.WriteLine($"--> Setting up consumer for queue: {_queueName}");
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                Console.WriteLine("========================================");
                Console.WriteLine("--> Event Received!");
                Console.WriteLine($"--> ConsumerTag: {ea.ConsumerTag}");
                Console.WriteLine($"--> DeliveryTag: {ea.DeliveryTag}");
                Console.WriteLine($"--> Exchange: {ea.Exchange}");
                Console.WriteLine($"--> RoutingKey: {ea.RoutingKey}");
                
                try
                {
                    var body = ea.Body.ToArray();
                    var notificationMessage = Encoding.UTF8.GetString(body);
                    
                    Console.WriteLine($"--> Message Content: {notificationMessage}");
                    Console.WriteLine("--> Processing event...");

                    _eventProcessor.ProcessEvent(notificationMessage);
                    
                    Console.WriteLine("--> Event processed successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"--> Error processing event: {ex.Message}");
                }
                
                Console.WriteLine("========================================");
                await Task.CompletedTask;
            };

            Console.WriteLine($"--> Starting to consume messages from queue: {_queueName}");
            await _channel.BasicConsumeAsync(
                queue: _queueName,
                autoAck: true,
                consumer: consumer,
                cancellationToken: stoppingToken
            );
            
            Console.WriteLine("--> Consumer started successfully. Waiting for messages...");

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private Task RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            Console.WriteLine("---> RabbitMQ Connection Shutdown");
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            Console.WriteLine("Message Bus Disposed");
            if(_channel?.IsOpen == true)
            {
                _channel.CloseAsync();
                _connection?.CloseAsync();
            }

            base.Dispose();
        }
    }
}

// this is a backgroud service that would contineously listen to the RabbitMq message bus. we intensionally didnt use the interface and concrete class method i.e creating an interface then implemeting it in a concrete class.