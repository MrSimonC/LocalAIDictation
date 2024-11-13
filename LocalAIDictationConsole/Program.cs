using AnythingLLMApi;
using AnythingLLMApi.Models;
using LocalAIDictationConsole;
using LocalAIDictationToLLM;
using TextCopy;

// Set up
string whisperServerIp = Environment.GetEnvironmentVariable("WHISPER_SERVER_IP") ?? "localhost";
string whisperInitialPrompt = EnvironmentVariableHelper.GetEnvironmentVariableFileContents("WHISPER_AI_INITIAL_PROMPT_PATH", true);
string whisperPostProcessingCsv = EnvironmentVariableHelper.GetEnvironmentVariableFileContents("WHISPER_AI_POST_PROCESSING_PATH", true);

// Voice
var voiceToAi = new VoiceToAi(whisperServerIp);
Console.WriteLine("--- Listening ---");
voiceToAi.VoiceInputRecordVoice();
Console.ReadKey(true);
Console.WriteLine("--- Processing ---");
string initialTextDictation = await voiceToAi.VoiceProcessRecordingToTextAsync(whisperInitialPrompt);

// CSV processing
string textDictation = PostProcessWhisperWithCSV(whisperPostProcessingCsv, initialTextDictation);
textDictation = textDictation.Trim();
await ClipboardService.SetTextAsync(textDictation);
Console.WriteLine("--- Raw transcription copied to clipboard ---");

// Augment with AI
var anythingLlmApiKey = Environment.GetEnvironmentVariable("ANYTHING_LLM_API_KEY");
ArgumentNullException.ThrowIfNull(anythingLlmApiKey);
var workspaceName = Environment.GetEnvironmentVariable("ANYTHING_LLM_DICTATION_WORKSPACE_NAME");
ArgumentNullException.ThrowIfNull(workspaceName);
var anythingLlmApiChat = new AnythingLlmApi("http://localhost:3001/api/", anythingLlmApiKey);
await anythingLlmApiChat.AuthAsync();

string InstructionPrompt = EnvironmentVariableHelper.GetEnvironmentVariableFileContents("AI_PROMPT_DICTATION");
string promptWithDictation = string.Format(InstructionPrompt, textDictation);

var workspaces = await anythingLlmApiChat.GetWorkspacesAsync();
var workspaceSlug = workspaces?.Workspaces.First(x => x.Name == workspaceName).Slug ?? throw new ArgumentNullException($"{workspaceName} not found");
string sessionId = Guid.NewGuid().ToString();
var workspaceChatRequest = new WorkspaceChatRequest
{
    Message = promptWithDictation,
    SessionId = sessionId
};
var response = await anythingLlmApiChat.ChatWithWorkspaceAsync(workspaceSlug, workspaceChatRequest);
ArgumentNullException.ThrowIfNull(response);
await ClipboardService.SetTextAsync(response.TextResponse);
return;

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