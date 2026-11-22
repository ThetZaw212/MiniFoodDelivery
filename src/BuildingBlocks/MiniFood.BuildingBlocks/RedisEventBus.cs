using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MiniFood.BuildingBlocks;

public interface IEventBus
{
    Task<long> PublishAsync<T>(string channel, string eventName, T payload, CancellationToken cancellationToken = default);
}

public sealed class RedisEventBus(IConnectionMultiplexer redis, ILogger<RedisEventBus> logger) : IEventBus
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<long> PublishAsync<T>(string channel, string eventName, T payload, CancellationToken cancellationToken = default)
    {
        var message = new EventMessage(eventName, JsonSerializer.Serialize(payload, Json), DateTime.UtcNow);
        var body = JsonSerializer.Serialize(message, Json);
        var receivers = await redis.GetSubscriber().PublishAsync(RedisChannel.Literal(channel), body);
        logger.LogInformation(
            "Async publish {Event} on {Channel}. Redis delivered it to {Receivers} subscriber(s).",
            eventName,
            channel,
            receivers);
        return receivers;
    }
}

public static class RedisSubscriber
{
    public static async Task ListenAsync(
        string channel,
        Func<string, Task> handler,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            IConnectionMultiplexer? connection = null;
            try
            {
                connection = await ConnectionMultiplexer.ConnectAsync(Infrastructure.Redis);
                var subscriber = connection.GetSubscriber();
                await subscriber.SubscribeAsync(RedisChannel.Literal(channel), (redisChannel, value) =>
                {
                    var json = value.ToString();
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await handler(json);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Handler failed for Redis channel {Channel}", channel);
                        }
                    }, cancellationToken);
                });

                logger.LogInformation("Async subscribe: listening on Redis channel {Channel}", channel);
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis listener for {Channel} dropped. Retrying in 3 seconds.", channel);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            finally
            {
                connection?.Dispose();
            }
        }
    }
}
