using NAudio.Wave;
using OllamaSharp;
using System.Net.Http.Headers;

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

        public async Task<string> VoiceProcessRecordingToTextAsync()
        {
            WaveIn.StopRecording();
            string? transcription = await CallWhisperApiAsync(OutputWaveFilePath);
            return transcription ?? string.Empty;
        }

        public static async Task<(string streamedText, ConversationContext? context)> CallOllamaModelApi(string? prompt, ConversationContext? context = null)
        {
            var uri = new Uri(OllamaApiUrl);
            var ollama = new OllamaApiClient(uri)
            {
                SelectedModel = "mistral"
            };

            // keep reusing the context to keep the chat topic going
            string streamedText = ""; // Variable to store the streamed text

            context = await ollama.StreamCompletion(prompt, context, stream =>
            {
                Console.Write(stream.Response);
                streamedText += stream.Response; // Append the streamed text to the variable
            });

            return (streamedText, context); // Return the full streamed text and the context
        }

        private static async Task<string?> CallWhisperApiAsync(string outputWaveFilePath)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(File.ReadAllBytes(outputWaveFilePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");

            content.Add(fileContent, "audio_file", Path.GetFileName(outputWaveFilePath));

            var response = await client.PostAsync(WhisperApiUrl, content);

            string responseString = await response.Content.ReadAsStringAsync();
            return responseString;
        }
    }
}
