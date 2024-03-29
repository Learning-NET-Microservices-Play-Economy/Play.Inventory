﻿using Mozart.Play.Common.MassTransit;
using Mozart.Play.Common.MongoDb;
using Mozart.Play.Inventory.Service.Clients;
using Mozart.Play.Inventory.Service.Entities;
using Polly;
using Polly.Timeout;

namespace Mozart.Play.Inventory.Service;

public class Program
{
    private const string AllowedOriginSetting = "AllowedOrigin";

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;
        var configuration = builder.Configuration;

        // Add services to the container.
        services.AddMongo()
                .AddMongoRepository<InventoryItem>("inventory.items")
                .AddMongoRepository<CatalogItem>("catalog.items")
                .AddMassTransitWithRabbitMQ();

        AddCatalogClient(services);

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

            app.UseCors(builder =>
            {
                builder.WithOrigins(configuration[AllowedOriginSetting])
                        .AllowAnyHeader()
                        .AllowAnyMethod();
            });
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }

    private static void AddCatalogClient(IServiceCollection services)
    {
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
    }
}

