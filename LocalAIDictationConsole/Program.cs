using LocalAIDictationToLLM;
using OllamaSharp;
using TextCopy;

var voiceToAi = new VoiceToAi();

// Voice
ConversationContext? context = null;
Console.WriteLine("--- Press any key to stop recording... ---");
voiceToAi.VoiceInputRecordVoice();
Console.ReadKey(true);
string textDictation = await voiceToAi.VoiceProcessRecordingToTextAsync();
Console.WriteLine(textDictation);
ClipboardService.SetText(textDictation);
Console.WriteLine("--- Dictation copied to clipboard. ---");

// AI - Load Optional Context
string? filePath = Environment.GetEnvironmentVariable("INITIAL_BASE_AI_CONTEXT_PATH");
string initialPrompt = string.Empty;
if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
{
    Console.WriteLine("--- (Initial AI base context file found and loaded ---");
    initialPrompt = File.ReadAllText(filePath);
}
else
{
    Console.WriteLine("--- (Initial AI base context file *not* found or not specified) ---");
}

// AI - Call LLM
Console.WriteLine("---Processing LLM. ---");
(string streamedText, _) = await VoiceToAi.CallOllamaModelApi(initialPrompt + textDictation, context);
await ClipboardService.SetTextAsync(streamedText.Trim());
Console.ReadKey(true);
