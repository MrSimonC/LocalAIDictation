using NAudio.Wave;
using System.Net.Http.Headers;

namespace LocalAIDictationToLLM;

public class VoiceToAi(string? whisperServerIp = "localhost")
{
    private const string OutputWaveFilePath = "output.wav";

    public WaveInEvent WaveIn { get; set; } = new WaveInEvent();

    public void VoiceInputRecordVoice()
    {
        var writer = new WaveFileWriter(OutputWaveFilePath, WaveIn.WaveFormat);
        WaveIn.DataAvailable += (sender, args) => writer.Write(args.Buffer, 0, args.BytesRecorded);
        WaveIn.RecordingStopped += (sender, args) => writer.Dispose();
        WaveIn.StartRecording();
    }

    public async Task<string> VoiceProcessRecordingToTextAsync(string? initialPrompt = null)
    {
        WaveIn.StopRecording();
        string? transcription = await CallWhisperApiAsync(OutputWaveFilePath, initialPrompt);

        // Delete the output wave file
        File.Delete(OutputWaveFilePath);

        return transcription ?? string.Empty;
    }

    private async Task<string?> CallWhisperApiAsync(string outputWaveFilePath, string? initialPrompt = null)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(outputWaveFilePath));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");

        content.Add(fileContent, "audio_file", Path.GetFileName(outputWaveFilePath));
        if (!string.IsNullOrEmpty(initialPrompt))
        {
            content.Add(new StringContent(initialPrompt), "initial_prompt");
        }

        string whisperApiUrl = $"http://{whisperServerIp}:9000/asr?encode=true&task=transcribe&language=en&word_timestamps=false&output=txt";
        var response = await client.PostAsync(whisperApiUrl, content);

        string responseString = await response.Content.ReadAsStringAsync();
        return responseString;
    }
}
