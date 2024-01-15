using NAudio.Wave;
using OllamaSharp;
using System.Net.Http.Headers;
using TextCopy;

string outputWaveFilePath = "output.wav";
var waveIn = new WaveInEvent();
var writer = new WaveFileWriter(outputWaveFilePath, waveIn.WaveFormat);
waveIn.DataAvailable += (sender, args) => writer.Write(args.Buffer, 0, args.BytesRecorded);
waveIn.RecordingStopped += (sender, args) => writer.Dispose();
waveIn.StartRecording();

string? clipboard = await ClipboardService.GetTextAsync();
string initialPrompt = $"I want you to act as a polite professional office worker. Assume the text within 3 backticks is the context. ```{clipboard}```. Using the context, ";

// Wait for user input to stop recording
Console.WriteLine("---Press any key to stop recording...");
Console.ReadKey(false);
waveIn.StopRecording();
Console.WriteLine("---Processing Audio.");
string? transcription = await CallEndpoint(outputWaveFilePath);
Console.WriteLine(transcription);
Console.WriteLine("---Processing LLM.");
await LocalLLM(initialPrompt + transcription);
Console.ReadKey(false);

static async Task<string?> CallEndpoint(string outputWaveFilePath)
{
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    using var content = new MultipartFormDataContent();
    var fileContent = new ByteArrayContent(File.ReadAllBytes(outputWaveFilePath));
    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");

    content.Add(fileContent, "audio_file", Path.GetFileName(outputWaveFilePath));

    var response = await client.PostAsync("http://localhost:9000/asr?encode=true&task=transcribe&language=en&word_timestamps=false&output=txt", content);

    string responseString = await response.Content.ReadAsStringAsync();
    return responseString;
}

static async Task LocalLLM(string? transcription)
{
    var uri = new Uri("http://localhost:11434");
    var ollama = new OllamaApiClient(uri)
    {
        SelectedModel = "mistral"
    };

    // keep reusing the context to keep the chat topic going
    ConversationContext? context = null;
    context = await ollama.StreamCompletion(transcription, context, stream => Console.Write(stream.Response));
}