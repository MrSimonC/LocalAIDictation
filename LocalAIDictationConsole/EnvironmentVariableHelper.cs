namespace LocalAIDictationConsole;

internal static class EnvironmentVariableHelper
{
    public static string GetEnvironmentVariableFileContents(string environmentVariableName, bool throwIfContentsEmpty = true)
    {
        string? filePath = Environment.GetEnvironmentVariable(environmentVariableName);
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            return File.ReadAllText(filePath);
        }
        else
        {
            return throwIfContentsEmpty ? throw new ArgumentNullException(environmentVariableName) : string.Empty;
        }
    }
}
