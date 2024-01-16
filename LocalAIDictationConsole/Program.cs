using LocalAIDictationToLLM;
using OllamaSharp;
using TextCopy;

// Voice
ConversationContext? context = null;
Console.WriteLine("--- Press any key to stop recording... ---");
string textDictation = await VoiceToAi.VoiceInputTextOutput();
Console.WriteLine(textDictation);
ClipboardService.SetText(textDictation);
Console.WriteLine("--- Dictation copied to clipboard. ---");

// AI - Load Optional Context
string? filePath = Environment.GetEnvironmentVariable("INITIAL_BASE_AI_CONTEXT_PATH");
string initialPrompt = string.Empty;
if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
{
    Console.WriteLine("--- (Initial AI base context file found and loaded ---");
    initialPrompt = File.ReadAllText(filePath) + $" Assume the text within 3 backticks is the context:Using the context, ";
}
else
{
    Console.WriteLine("--- (Initial AI base context file *not* found or not specified) ---");
}

// AI - Call LLM
Console.WriteLine("---Processing LLM. ---");
(string streamedText, _) = await VoiceToAi.CallOllamaModelApi(initialPrompt + textDictation, context);
await ClipboardService.SetTextAsync(streamedText.Trim());
_ = Console.ReadKey(true);
