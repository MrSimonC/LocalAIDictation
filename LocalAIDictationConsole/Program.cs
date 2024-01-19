using LocalAIDictationConsole;
using LocalAIDictationToLLM;
using OllamaSharp;
using TextCopy;

// Environment Variables
string whisperInitialPrompt = EnvironmentVariableHelper.GetEnvironmentVariableFileContents("WHISPER_AI_INITIAL_PROMPT_PATH");
string whisperPostProcessingCsv = EnvironmentVariableHelper.GetEnvironmentVariableFileContents("WHISPER_AI_POST_PROCESSING_PATH");
string ollamaBaseContext = EnvironmentVariableHelper.GetEnvironmentVariableFileContents("OLLAMA_BASE_CONTEXT_PATH");
string ollamaModel = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "phi";
string foundWhisperInitialPrompt = !string.IsNullOrEmpty(whisperInitialPrompt) ? "with" : "without";
string foundWhisperPostProcessingCsv = !string.IsNullOrEmpty(whisperPostProcessingCsv) ? "with" : "without";
string foundOllamaBaseContext = !string.IsNullOrEmpty(ollamaBaseContext) ? "with" : "without"; 
string foundOllamaModel = !string.IsNullOrEmpty(ollamaModel) ? "with" : "with default";

var voiceToAi = new VoiceToAi();

// Get Clipboard before recording
string clipboardText = ClipboardService.GetText() ?? string.Empty;

// Voice
ConversationContext? context = null;
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

// Local LLM
ollamaBaseContext = string.Format(ollamaBaseContext, clipboardText); // replace {0} in INITIAL_BASE_AI_CONTEXT_PATH file the with clipboard text


Console.WriteLine($"--- Processing {foundOllamaModel} {ollamaModel} LLM model {foundOllamaBaseContext} context ---");
string prompt = ollamaBaseContext + textDictation;
#if DEBUG
Console.WriteLine($"\n\n--- DEBUG: Prompt is {prompt} ---\n\n");
#endif
(string streamedText, _) = await VoiceToAi.CallOllamaModelApi(ollamaModel, prompt, context);
await ClipboardService.SetTextAsync(streamedText.Trim());