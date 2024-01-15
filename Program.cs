using NAudio.Wave;
using System.Net.Http.Headers;

string outputWaveFilePath = "output.wav";
var waveIn = new WaveInEvent();
var writer = new WaveFileWriter(outputWaveFilePath, waveIn.WaveFormat);
waveIn.DataAvailable += (sender, args) => writer.Write(args.Buffer, 0, args.BytesRecorded);
waveIn.RecordingStopped += (sender, args) => writer.Dispose();
waveIn.StartRecording();

// Wait for user input to stop recording
Console.WriteLine("Press any key to stop recording...");
Console.ReadKey();
waveIn.StopRecording();
string? transcription = await CallEndpoint(outputWaveFilePath);
Console.WriteLine(transcription);
Console.ReadKey();

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
