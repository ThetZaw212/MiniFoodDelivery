namespace MiniFood.BuildingBlocks;

public static class Infrastructure
{
    public static string Sql(string database)
    {
        var host = Environment.GetEnvironmentVariable("SQL_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("SQL_PORT") ?? "1433";
        var user = Environment.GetEnvironmentVariable("SQL_USER") ?? "sa";
        var password = Environment.GetEnvironmentVariable("SQL_SA_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "SQL_SA_PASSWORD is empty. Copy .env.example to .env and set the SA password of your sqlserver2022 container.");
        }

        return $"Server={host},{port};Database={database};User Id={user};Password={password};TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=True";
    }

    public static string Redis
    {
        get
        {
            var host = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
            var port = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
            return $"{host}:{port},abortConnect=false,connectRetry=5";
        }
    }

    public static string Required(string name, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
