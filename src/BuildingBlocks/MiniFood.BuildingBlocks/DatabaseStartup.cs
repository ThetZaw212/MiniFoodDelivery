using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MiniFood.BuildingBlocks;

public static class DatabaseStartup
{
    public static async Task EnsureCreatedAsync(DbContext db, ILogger logger, Func<Task>? seed = null)
    {
        for (var attempt = 1; attempt <= 15; attempt++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync();
                if (seed is not null)
                    await seed();
                logger.LogInformation("SQL database is ready");
                return;
            }
            catch (Exception ex) when (attempt < 15)
            {
                logger.LogWarning(ex, "SQL is not ready (attempt {Attempt}/15). Retrying in 2 seconds.", attempt);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }
}
