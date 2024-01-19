using LocalAIDictationToLLM;
using OllamaSharp;
using TextCopy;

// Environment Variables
string whisperInitialPrompt = GetEnvironmentVariableFileContents("WHISPER_AI_INITIAL_PROMPT_PATH");
string whisperPostProcessingCsv = GetEnvironmentVariableFileContents("WHISPER_AI_POST_PROCESSING_PATH");
string ollamaBaseContext = GetEnvironmentVariableFileContents("OLLAMA_BASE_CONTEXT_PATH");
string? ollamaModel = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "phi";

var voiceToAi = new VoiceToAi();

// Get Clipboard before recording
string clipboardText = ClipboardService.GetText() ?? string.Empty;

// Voice
ConversationContext? context = null;
string foundWhisperInitialPrompt = !string.IsNullOrEmpty(whisperInitialPrompt) ? "with" : "without";
string foundWhisperPostProcessingCsv = !string.IsNullOrEmpty(whisperPostProcessingCsv) ? "with" : "without";
Console.WriteLine($"--- Recording {foundWhisperInitialPrompt} context {foundWhisperPostProcessingCsv} post processing ---");
Console.WriteLine("Press Space for dictation only, or any other key to use local AI ---");
voiceToAi.VoiceInputRecordVoice();
var keyPressed = Console.ReadKey(true);
Console.WriteLine("--- Processing voice ---");
string textDictation = await voiceToAi.VoiceProcessRecordingToTextAsync(whisperInitialPrompt);

if (!string.IsNullOrEmpty(whisperPostProcessingCsv))
{
    string[] initialWhisperPromptCsvLines = whisperPostProcessingCsv.Split("\r\n");
    for (int i = 1; i < initialWhisperPromptCsvLines.Length; i++)
    {
        string[] initialWhisperPromptCsvLine = initialWhisperPromptCsvLines[i].Split(",");
        string firstColumnValue = initialWhisperPromptCsvLine[0];
        if (firstColumnValue == "{newline}") // special self-made pattern representing a new line
        {
            firstColumnValue = "\n";
        }
        textDictation = textDictation.Replace(firstColumnValue, initialWhisperPromptCsvLine[1]);
    }
}
Console.WriteLine("--- Processing done. You said ---");
Console.WriteLine(textDictation);
ClipboardService.SetText(textDictation);
Console.WriteLine("--- Dictation copied to clipboard ---");
if (keyPressed.Key == ConsoleKey.Spacebar)
{
    return;
}

// AI - Load Optional Context
ollamaBaseContext = string.Format(ollamaBaseContext, clipboardText); // replace {0} in INITIAL_BASE_AI_CONTEXT_PATH file the with clipboard text
string foundOllamaBaseContext = !string.IsNullOrEmpty(ollamaBaseContext) ? "with " : "without";
#if DEBUG
Console.WriteLine($"---( Initial AI base context is: {ollamaBaseContext} )---");
#endif

Console.WriteLine($"--- Processing {ollamaModel} LLM {foundOllamaBaseContext} context ---");
string prompt = ollamaBaseContext + textDictation;
(string streamedText, _) = await VoiceToAi.CallOllamaModelApi(ollamaModel, prompt, context);
await ClipboardService.SetTextAsync(streamedText.Trim());

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