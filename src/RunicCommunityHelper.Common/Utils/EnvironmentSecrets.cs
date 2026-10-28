namespace RunicCommunityHelper.Common.Utils;

public static class EnvironmentSecrets
{
    public static string? Get(string name)
    {
        var filePath = Environment.GetEnvironmentVariable($"{name}_FILE");
        if (!string.IsNullOrEmpty(filePath))
        {
            return File.ReadAllText(filePath).Trim();
        }

        return Environment.GetEnvironmentVariable(name);
    }
}
