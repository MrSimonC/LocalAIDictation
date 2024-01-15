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
string initialPrompt = $"My name is Simon Crouch and I want you to act as me. My tone is casual, professional, slightly indirect yet enthusiastic. Assume the text within 3 backticks is the context: ```{clipboard}```. Using the context, ";

// Wait for user input to stop recording
Console.WriteLine("---Press any key to stop recording...");
Console.ReadKey(true);
waveIn.StopRecording();
Console.WriteLine("---Processing Audio.");
string? transcription = await CallEndpoint(outputWaveFilePath);
Console.WriteLine(transcription);
Console.WriteLine("---Processing LLM.");
(string streamedText, var context) = await LocalLLM(initialPrompt + transcription);
await ClipboardService.SetTextAsync(streamedText.Trim());
Console.ReadKey(true);

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

static async Task<(string streamedText, ConversationContext? context)> LocalLLM(string? transcription)
{
    var uri = new Uri("http://localhost:11434");
    var ollama = new OllamaApiClient(uri)
    {
        SelectedModel = "mistral"
    };

    // keep reusing the context to keep the chat topic going
    ConversationContext? context = null;
    string streamedText = ""; // Variable to store the streamed text

    context = await ollama.StreamCompletion(transcription, context, stream =>
    {
        Console.Write(stream.Response);
        streamedText += stream.Response; // Append the streamed text to the variable
    });

    return (streamedText, context); // Return the full streamed text and the context
}
