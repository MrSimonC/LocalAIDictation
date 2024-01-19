namespace LocalAIDictationConsole;

internal static class EnvironmentVariableHelper
{
    public static string GetEnvironmentVariableFileContents(string environmentVariableName)
    {
        string? filePath = Environment.GetEnvironmentVariable(environmentVariableName);
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            return File.ReadAllText(filePath);
        }
        else
        {
            return string.Empty;
        }
    }
}
