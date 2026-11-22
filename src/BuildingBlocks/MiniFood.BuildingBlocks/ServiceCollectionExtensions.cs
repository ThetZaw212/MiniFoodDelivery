using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace MiniFood.BuildingBlocks;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMiniFoodRedis(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(Infrastructure.Redis));
        services.AddSingleton<IEventBus, RedisEventBus>();
        return services;
    }
}
