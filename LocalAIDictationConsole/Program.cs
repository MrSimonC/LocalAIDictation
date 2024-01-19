using LocalAIDictationToLLM;
using OllamaSharp;
using TextCopy;

var voiceToAi = new VoiceToAi();

// Get Clipboard before recording
string clipboardText = ClipboardService.GetText() ?? string.Empty;

// Voice
string initialWhisperPrompt = GetEnvironmentVariableFileContents("WHISPER_AI_INITIAL_PROMPT_PATH"); // 224 max tokens allowed by whisper ai
ConversationContext? context = null;
string foundInitialWhisperPrompt = !string.IsNullOrEmpty(initialWhisperPrompt) ? " with context" : string.Empty;
Console.WriteLine($"--- Recording{foundInitialWhisperPrompt}. Press Space for dictation only, or any other key to use local AI ---");
voiceToAi.VoiceInputRecordVoice();
var keyPressed = Console.ReadKey(true);
Console.WriteLine("--- Processing voice ---");
string textDictation = await voiceToAi.VoiceProcessRecordingToTextAsync(initialWhisperPrompt);
textDictation = textDictation.Replace("\n", " ");

// read csv at path WHISPER_AI_POST_PROCESSING_PATH replacing anything found in column 1, with column 2
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
    Console.WriteLine("--- (No post processing file found) ---");
}

ClipboardService.SetText(textDictation);
Console.WriteLine("--- Dictation copied to clipboard ---");
// if keyPressed = space, then exit the program
if (keyPressed.Key == ConsoleKey.Spacebar)
{
    Console.WriteLine("--- Exiting program ---");
    return;
}

// AI - Load Optional Context
string initialBaseAIContext = GetEnvironmentVariableFileContents("INITIAL_BASE_AI_CONTEXT_PATH");
initialBaseAIContext = string.Format(initialBaseAIContext, clipboardText); // replace {0} in INITIAL_BASE_AI_CONTEXT_PATH file the with clipboard text
string foundInitialBaseAIContext = !string.IsNullOrEmpty(initialBaseAIContext) ? "with context " : string.Empty;
#if DEBUG
    Console.WriteLine($"---( Initial AI base context is: {initialBaseAIContext} )---");
#endif
Console.WriteLine($"--- Processing LLM {foundInitialBaseAIContext}---");
(string streamedText, _) = await VoiceToAi.CallOllamaModelApi(initialBaseAIContext + textDictation, context);
await ClipboardService.SetTextAsync(streamedText.Trim());
Console.ReadKey(true);

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