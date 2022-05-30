using Mozart.Play.Common.MongoDb;
using Mozart.Play.Inventory.Service.Clients;
using Mozart.Play.Inventory.Service.Entities;
using Polly;
using Polly.Timeout;

namespace Mozart.Play.Inventory.Service;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;

        // Add services to the container.
        services.AddMongo();
        services.AddMongoRepository<InventoryItem>("inventory.items");

        Random jitterer = new Random();

        services.AddHttpClient<CatalogClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7273");
        })
        .AddTransientHttpErrorPolicy(builder =>
            builder.Or<TimeoutRejectedException>().WaitAndRetryAsync(
                5, // Number of retries after failing
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(jitterer.Next(0, 1000)), // Wait time before another retry
                onRetry: (outcome, timeSpan, retryAttempt) =>
                {
                    var serviceProvider = services.BuildServiceProvider();
                    serviceProvider.GetService<ILogger<CatalogClient>>()?
                        .LogWarning($"Delaying for {timeSpan.TotalSeconds} seconds, then making retry {retryAttempt}");
                })
        )
        .AddTransientHttpErrorPolicy(builder =>
        builder.Or<TimeoutRejectedException>().CircuitBreakerAsync(
            3, // Allow up to three fails
            TimeSpan.FromSeconds(15), // Block requests for 13 seconds
            onBreak: (outcome, timeSpan) =>
            {
                var serviceProvider = services.BuildServiceProvider();
                serviceProvider.GetService<ILogger<CatalogClient>>()?
                    .LogWarning($"Opening the circuit for {timeSpan.TotalSeconds} seconds...");
            },
            onReset: () =>
            {
                var serviceProvider = services.BuildServiceProvider();
                serviceProvider.GetService<ILogger<CatalogClient>>()?
                    .LogWarning($"Closing the circuit...");
            }
            )
        )
        .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));

        services.AddControllers(options =>
        {
            options.SuppressAsyncSuffixInActionNames = false;
        });

        services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}

