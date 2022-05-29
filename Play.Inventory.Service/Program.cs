using Mozart.Play.Common.MongoDb;
using Mozart.Play.Common.Settings;
using Mozart.Play.Inventory.Service.Entities;

namespace Mozart.Play.Inventory.Service;

public class Program
{
    private static ServiceSettings serviceSettings;

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();

        builder.Services.AddMongo();
        builder.Services.AddMongoRepository<InventoryItem>("inventory.items");

        builder.Services.AddControllers(options =>
        {
            options.SuppressAsyncSuffixInActionNames = false;
        });

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

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

