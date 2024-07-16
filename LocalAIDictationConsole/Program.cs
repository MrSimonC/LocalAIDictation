using LocalAIDictationConsole;
using LocalAIDictationToLLM;
using OllamaSharp;
using TextCopy;

// Whisper
string whisperServerIp = Environment.GetEnvironmentVariable("WHISPER_SERVER_IP") ?? "localhost";
string whisperInitialPrompt = EnvironmentVariableHelper.GetEnvironmentVariableFileContents("WHISPER_AI_INITIAL_PROMPT_PATH", true);
string whisperPostProcessingCsv = EnvironmentVariableHelper.GetEnvironmentVariableFileContents("WHISPER_AI_POST_PROCESSING_PATH", true);
// Ollama
string ollamaServerIp = Environment.GetEnvironmentVariable("OLLAMA_SERVER_IP") ?? "localhost";
string ollamaModel = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "phi3";
// Prompts
string promptDictation = EnvironmentVariableHelper.GetEnvironmentVariableFileContents("AI_PROMPT_DICTATION");
string promptActAsMe = EnvironmentVariableHelper.GetEnvironmentVariableFileContents("AI_PROMPT_ACT_AS_ME");
// Context
string baseContext = EnvironmentVariableHelper.GetEnvironmentVariableFileContents("AI_BASE_CONTEXT");

// Voice
var voiceToAi = new VoiceToAi(whisperServerIp, ollamaServerIp);
ConversationContext? context = null;
Console.WriteLine("--- Recording ---\nSpace:\tDictation (Post Processed)\nd:\tDictation only\na:\tAct as me\nc:\tAct as me (with Clipboard)");
voiceToAi.VoiceInputRecordVoice();
var keyPressed = Console.ReadKey(true);
Console.WriteLine("--- Processing ---");
string initialTextDictation = await voiceToAi.VoiceProcessRecordingToTextAsync(whisperInitialPrompt);
string textDictation = PostProcessWhisperWithCSV(whisperPostProcessingCsv, initialTextDictation);
// Clipboard
string originalClipboardText = await ClipboardService.GetTextAsync() ?? string.Empty;
// Always copy light-post-processed dictated text to clipboard
await ClipboardService.SetTextAsync(textDictation.Trim());
// Prompt
string prompt = string.Empty;
if (keyPressed.Key == ConsoleKey.Spacebar)
{
    prompt = CreatePrompt(baseContext, promptDictation, textDictation);
}
else if (keyPressed.Key == ConsoleKey.D) // output dictation alone, then exit
{
    await ClipboardService.SetTextAsync(textDictation.Trim());
    return;
}
else if (keyPressed.Key == ConsoleKey.C) // use the clipboard with context
{
    string contextWithClipboard = baseContext + "\n\n" + originalClipboardText;
    prompt = CreatePrompt(contextWithClipboard, promptActAsMe, textDictation);
}
else
{
    prompt = CreatePrompt(baseContext, promptActAsMe, textDictation);
}
// LLM Processing
(string streamedText, ConversationContext? _) = await voiceToAi.CallOllamaModelApi(ollamaModel, prompt, context);
await ClipboardService.SetTextAsync(streamedText.Trim());

#if DEBUG
string whisperInitialPromptFound = !string.IsNullOrEmpty(whisperInitialPrompt) ? " (with prompt)" : "";
string whisperPostProcessingCsvFound = !string.IsNullOrEmpty(whisperPostProcessingCsv) ? " (with post)" : "";
DebugOutput(whisperInitialPromptFound, whisperPostProcessingCsvFound, textDictation, prompt, initialTextDictation);

static void DebugOutput(string whisperInitialPromptFound, string whisperPostProcessingCsvFound, string textDictation, string prompt, string initialTextDictation)
{
    Console.WriteLine($"\n\n\n\n--- DEBUG Processing voice {whisperInitialPromptFound}{whisperPostProcessingCsvFound} ---");
    Console.WriteLine("--- Initial Voice understanding: ---");
    Console.WriteLine(initialTextDictation);
    Console.WriteLine("--- Post CSV processing Voice understanding: ---");
    Console.WriteLine(textDictation);
    Console.WriteLine($"\n---<PROMPT>---\n{prompt}\n---</PROMPT>---\n");
}
#endif

static string CreatePrompt(string baseContext, string basePrompt, string vocalPrompt)
{
    string baseContextWrapper = $"The text between three percentages is the main context: %%%{baseContext}%%%.\n\n";
    string combinedPrompt = string.Format(basePrompt, vocalPrompt);
    return baseContextWrapper + combinedPrompt;
}

static string PostProcessWhisperWithCSV(string whisperPostProcessingCsv, string textDictation)
{
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

    return textDictation;
}