using LocalAIDictationToLLM;
using OllamaSharp;
using TextCopy;

var voiceToAi = new VoiceToAi();

// Get Clipboard before recording
string clipboardText = ClipboardService.GetText() ?? string.Empty;

// Voice
string initialWhisperPrompt = GetEnvironmentVariableFileContents("WHISPER_AI_INITIAL_PROMPT_PATH"); // 224 max tokens allowed by whisper ai
ConversationContext? context = null;
string foundContext = !string.IsNullOrEmpty(initialWhisperPrompt) ? " with context" : string.Empty;
Console.WriteLine($"--- Recording{foundContext}. Press Space for dictation only, or any other to push to local AI... ---");
voiceToAi.VoiceInputRecordVoice();
var keyPressed = Console.ReadKey(true);
Console.WriteLine("--- Processing voice ---");
string textDictation = await voiceToAi.VoiceProcessRecordingToTextAsync(initialWhisperPrompt);
textDictation = textDictation.Replace("\n", " ");

// write code to read in WHISPER_AI_POST_PROCESSING_PATH csv file using GetEnvironmentVariableFileContents(), ignore the header (first row), then loop through entires, replacing anything found in column 1 from the csv with column 2 from the csv in textDictation
string initialWhisperPromptCsv = GetEnvironmentVariableFileContents("WHISPER_AI_POST_PROCESSING_PATH");
if (!string.IsNullOrEmpty(initialWhisperPromptCsv))
{
    Console.WriteLine("--- Performing Post Processing ---");
    string[] initialWhisperPromptCsvLines = initialWhisperPromptCsv.Split("\r\n");
    for (int i = 1; i < initialWhisperPromptCsvLines.Length; i++)
    {
        string[] initialWhisperPromptCsvLine = initialWhisperPromptCsvLines[i].Split(",");
        textDictation = textDictation.Replace(initialWhisperPromptCsvLine[0], initialWhisperPromptCsvLine[1]);
    }
}
else
{
    Console.WriteLine("--- No post processing file found ---");
}

ClipboardService.SetText(textDictation);
Console.WriteLine("--- Dictation copied to clipboard. ---");
// if keyPressed = space, then exit the program
if (keyPressed.Key == ConsoleKey.Spacebar)
{
    Console.WriteLine("--- Exiting program. ---");
    return;
}

// AI - Load Optional Context
string initialPrompt = GetEnvironmentVariableFileContents("INITIAL_BASE_AI_CONTEXT_PATH");
initialPrompt = string.Format(initialPrompt, clipboardText); // replace {0} in INITIAL_BASE_AI_CONTEXT_PATH file the with clipboard text
if (string.IsNullOrEmpty(initialPrompt))
{
    Console.WriteLine("--- (Initial AI base context file *not* found or not specified) ---");
}
#if DEBUG
    Console.WriteLine($"---( Initial AI base context is: {initialPrompt} )---");
#endif

// AI - Call LLM
Console.WriteLine("---Processing LLM. ---");
(string streamedText, _) = await VoiceToAi.CallOllamaModelApi(initialPrompt + textDictation, context);
await ClipboardService.SetTextAsync(streamedText.Trim());
Console.ReadKey(true);

// create a local method which takes in an environment variable name which is a file path, reads the file if it exists, and returns either the file contents or an empty string
static string GetEnvironmentVariableFileContents(string environmentVariableName)
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