namespace MiniFood.BuildingBlocks;

public static class DotEnv
{
    public static void Load()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(start);
            while (dir is not null)
            {
                var path = Path.Combine(dir.FullName, ".env");
                if (File.Exists(path))
                {
                    Apply(path);
                    return;
                }

                dir = dir.Parent;
            }
        }
    }

    private static void Apply(string path)
    {
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var split = line.IndexOf('=');
            if (split <= 0)
                continue;

            var key = line[..split].Trim();
            var value = line[(split + 1)..].Trim().Trim('"');
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                Environment.SetEnvironmentVariable(key, value);
        }
    }
}
