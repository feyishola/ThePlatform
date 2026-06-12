using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CommandsService.Models;
using Grpc.Net.Client;
using PlatformService;

namespace CommandsService.SyncDataServices.Grpc
{
    public class PlatformDataClient : IPlatformDataClient
    {
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;
        public PlatformDataClient(IConfiguration config, IMapper mapper)
        {
            _config = config;
            _mapper = mapper;
        }
        public IEnumerable<Platform> ReturnAllPlatforms()
        {
            var grpcPlatform = _config["GrpcPlatform"] ?? throw new InvalidOperationException("GrpcPlatform configuration is missing");
            Console.WriteLine($"--> Calling GRPC Service {grpcPlatform}");

            // var channel = GrpcChannel.ForAddress(grpcPlatform);

            var httpHandler = new HttpClientHandler();
            // Ignore certificate validation for development - ONLY for development!
            httpHandler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            var channel = GrpcChannel.ForAddress(grpcPlatform, new GrpcChannelOptions
            {
                HttpHandler = httpHandler
            });

            
            var client = new GrpcPlatform.GrpcPlatformClient(channel);
            var request = new GetAllRequest();

            try
            {
                var reply = client.GetAllPlatforms(request);
                return _mapper.Map<IEnumerable<Platform>>(reply.Platform);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not call GRPC Server: {ex.Message}");
                return Enumerable.Empty<Platform>();
            }
        }
    }
}