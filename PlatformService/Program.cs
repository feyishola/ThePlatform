using Microsoft.EntityFrameworkCore;
using PlatformService.AsyncDataServices;
using PlatformService.Data;
using PlatformService.SyncDataServices.Grpc;
using PlatformService.SyncDataServices.Http;

var builder = WebApplication.CreateBuilder(args);

// Configure database based on environment
var env = builder.Environment;

if (env.IsProduction())
{
    Console.WriteLine("--> Using SqlServer Db");
    // Use SQL Server in production
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("PlatformsConn")));
}
else
{
    Console.WriteLine("--> Using InMemory Db");
    // Use InMemory database in development/testing
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("InMem"));
}

builder.Services.AddHttpClient<ICommandDataClient, HttpCommandDataClient>();

builder.Services.AddSingleton<IMessageBusClient, MessageBusClient>();

builder.Services.AddGrpc();

builder.Services.AddScoped<IPlatformRepo, PlatformRepo>();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies()); // AutoMapper configuration, what it does is it automatically maps the data from one object to another object

builder.Services.AddControllers(); // Add controllers to the container for dependency injection, enabling dependency injection for controllers

var app = builder.Build();

PrepDb.PrepopulateDb(app, env.IsProduction());

app.MapGet("/", () => "Hello World!");
app.MapControllers(); // Wire up controller routes
app.MapGrpcService<GrpcPlatformService>(); // Wire up gRPC service

app.MapGet("/protos/platforms.proto", (context) =>
{
    return context.Response.WriteAsync(File.ReadAllText("protos/platforms.proto"));
});

app.Run();



// docker run -d -p 8080:80 -e ASPNETCORE_URLS=http://+:80 --name platformservice feyishola/platformservice
// docker run -p 8080:80 -d feyishola/platformservice 
// docker run -d -p 8080:8080 --name platformservice feyishola/platformservice


// minikube service platformnpservice-srv --url   this command gives you the url/port of the service in minikube
// because im using Docker driver on windows isolates the minikube cluster i.e the network from the host machine, the above url creates a temporary tunnel to access the NodePort service in minikube


//minikube addons enable ingress  this command enables ingress in minikube
//kubectl get namespaces  this command lists all namespaces in minikube (namespaces are like virtual clusters within a cluster)
//kubectl get pods --namespace=ingress-nginx this command lists all pods in the ingress-nginx namespace
//kubectl get services --namespace=ingress-nginx this command lists all services in the ingress-nginx namespace

// Windows\System32\drivers\etc\hosts   here is the location where i mapped 127.0.0.1 to kubernetesdomaintest.com


//minikube tunnel    >>>> If you want to access it on port 80 without specifying the port number i.e on domain kubernetesdomaintest.com, you can use minikube tunnel to create a route to services with LoadBalancer type

//kubectl get storageclass
//kubectl get pvc
//kubectl rollout restart <deployment> <platforms-depl>