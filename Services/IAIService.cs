using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using VoicePoC.Models;

namespace VoicePoC.Services;

public interface IAIService
{
    Task ProcessRecordingAsync(string recordingUrl, string callSid, string geminiKey, string apiKey, string apiSecret, CallData data);
}

public class GeminiService(IHttpClientFactory clientFactory) : IAIService
{
    public async Task ProcessRecordingAsync(string recordingUrl, string callSid, string geminiKey, string apiKey, string apiSecret, CallData data)
    {
        try
        {
            var httpClient = clientFactory.CreateClient();

            // --- Basic Authentication to download the file from Twilio ---
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:{apiSecret}"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            var audioUrl = recordingUrl.EndsWith(".mp3") ? recordingUrl : recordingUrl + ".mp3";
            var audioBytes = await httpClient.GetByteArrayAsync(audioUrl);
            var base64Audio = Convert.ToBase64String(audioBytes);

            // --- Reset Headers for Gemini Request ---
            httpClient.DefaultRequestHeaders.Authorization = null;

            var promptText = "Listen to this phone call. 1. Transcribe the conversation accurately. 2. Provide a short summary. 3. List any follow-up actions required.";

            var payload = new
            {
                contents = new[] {
                    new {
                        parts = new object[] {
                            new { text = promptText },
                            new { inline_data = new { mime_type = "audio/mp3", data = base64Audio } }
                        }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var aiResponse = await httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={geminiKey}",
                jsonContent);

            var responseString = await aiResponse.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseString);
            string resultText = "No response text found.";

            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var content = candidates[0].GetProperty("content");
                if (content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                {
                    resultText = parts[0].GetProperty("text").GetString() ?? "";
                }
            }
            else if (doc.RootElement.TryGetProperty("error", out var error))
            {
                resultText = "AI Error: " + error.GetProperty("message").GetString();
            }

            data.Status = "Completed";
            data.AiResponse = resultText;
        }
        catch (Exception ex)
        {
            data.Status = $"Error: {ex.Message}";
        }
    }
}
