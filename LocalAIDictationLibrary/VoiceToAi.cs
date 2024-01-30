using NAudio.Wave;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Streamer;
using System.Net.Http.Headers;
using System.Text.Json;
using static OllamaSharp.OllamaApiClient;

namespace LocalAIDictationToLLM
{
    public class VoiceToAi
    {
        private const string WhisperApiUrl = "http://localhost:9000/asr?encode=true&task=transcribe&language=en&word_timestamps=false&output=txt";
        private const string OllamaApiUrl = "http://localhost:11434";
        private const string OutputWaveFilePath = "output.wav";

        public WaveInEvent WaveIn { get; set; }

        public VoiceToAi() => WaveIn = new WaveInEvent();

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

        private static async Task<string?> CallWhisperApiAsync(string outputWaveFilePath, string? initialPrompt = null)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(File.ReadAllBytes(outputWaveFilePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");

            content.Add(fileContent, "audio_file", Path.GetFileName(outputWaveFilePath));
            if (!string.IsNullOrEmpty(initialPrompt))
            {
                content.Add(new StringContent(initialPrompt), "initial_prompt");
            }

            var response = await client.PostAsync(WhisperApiUrl, content);

            string responseString = await response.Content.ReadAsStringAsync();
            return responseString;
        }

        public static async Task<(string streamedText, ConversationContext? context)> CallOllamaModelApi(
            string model,
            string prompt,
            ConversationContext? context = null)
        {
            var uri = new Uri(OllamaApiUrl);
            var ollama = new OllamaApiClient(uri);

            // keep reusing the context to keep the chat topic going
            string streamedText = ""; // Variable to store the streamed text

            var generateRequest = new GenerateCompletionRequest
            {
                Prompt = prompt,
                Options = "{ \"temperature\": 0.9 }",
                Model = model
            };

            context = await ollama.StreamCompletion(generateRequest, new ActionResponseStreamer<GenerateCompletionResponseStream>(stream =>
            {
                Console.Write(stream.Response);
                streamedText += stream.Response; // Append the streamed text to the variable
            }));

            return (streamedText, context); // Return the full streamed text and the context
        }
    }
}
