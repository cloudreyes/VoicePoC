using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Twilio.Jwt.AccessToken;
using Twilio.TwiML;
using Twilio.TwiML.Voice;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add user secrets if an assembly user-secrets id is available (useful in development).
try
{
    var entryAssembly = Assembly.GetEntryAssembly();
    if (entryAssembly is not null)
    {
        builder.Configuration.AddUserSecrets(entryAssembly, optional: true);
    }
}
catch
{
    // If user secrets couldn't be added, continue without failing.
}

builder.Services.AddRazorPages();
builder.Services.AddHttpClient();

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

// --- IN-MEMORY STORAGE ---
var callStore = new ConcurrentDictionary<string, CallData>();

// 1. TOKEN ENDPOINT
app.MapPost("/token", ([FromBody] TokenRequest req) =>
{
    var grant = new VoiceGrant
    {
        OutgoingApplicationSid = req.TwimlAppSid,
        IncomingAllow = true
    };

    var token = new Token(
        accountSid: req.AccountSid,
        signingKeySid: req.ApiKey,
        secret: req.ApiSecret,
        identity: "browser-user",
        grants: new HashSet<IGrant> { grant }
    );

    return Results.Json(new { token = token.ToJwt() });
});

// 2. VOICE WEBHOOK
app.MapPost("/voice", (HttpRequest request) =>
{
    var response = new VoiceResponse();

    // Extract parameters passed from the Browser JS
    var numberToCall = request.Form.ContainsKey("To") ? request.Form["To"].ToString() : "";
    var callerId = request.Form.ContainsKey("CallerId") ? request.Form["CallerId"].ToString() : "";
    var geminiKey = request.Form.ContainsKey("GeminiKey") ? request.Form["GeminiKey"].ToString() : "";

    // --- NEW: Capture Credentials ---
    var apiKey = request.Form.ContainsKey("TwilioApiKey") ? request.Form["TwilioApiKey"].ToString() : "";
    var apiSecret = request.Form.ContainsKey("TwilioApiSecret") ? request.Form["TwilioApiSecret"].ToString() : "";

    if (!string.IsNullOrEmpty(numberToCall))
    {
        var dial = new Dial(callerId: callerId);
        dial.Number(numberToCall);

        dial.Record = Dial.RecordEnum.RecordFromRingingDual;

        var callbackUrl = $"/recording-completed?k={Uri.EscapeDataString(geminiKey)}&ak={Uri.EscapeDataString(apiKey)}&as={Uri.EscapeDataString(apiSecret)}";

        dial.RecordingStatusCallback = new Uri(callbackUrl, UriKind.Relative);

        response.Append(dial);
    }
    else
    {
        response.Say("No number provided.");
    }

    return Results.Content(response.ToString(), "application/xml");
});

// 3. RECORDING WEBHOOK (Updated with Authentication)
app.MapPost("/recording-completed", async (HttpRequest request, IHttpClientFactory clientFactory) =>
{
    var recordingUrl = request.Form["RecordingUrl"].ToString();
    var callSid = request.Form["CallSid"].ToString();

    // Retrieve keys from Query String
    var geminiKey = request.Query["k"].ToString();
    var apiKey = request.Query["ak"].ToString();
    var apiSecret = request.Query["as"].ToString();

    if (string.IsNullOrEmpty(recordingUrl)) return Results.Ok();

    var data = new CallData { CallSid = callSid, Status = "Processing AI...", RecordingUrl = recordingUrl };
    callStore[callSid] = data;

    _ = System.Threading.Tasks.Task.Run(async () =>
    {
        try
        {
            var httpClient = clientFactory.CreateClient();

            // --- FIX: Add Basic Authentication to download the file ---
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
            callStore[callSid] = data;
        }
        catch (Exception ex)
        {
            data.Status = $"Error: {ex.Message}";
            callStore[callSid] = data;
        }
    });

    return Results.Ok();
});

app.MapGet("/calls", () => Results.Json(callStore.Values.OrderByDescending(x => x.Timestamp)));

app.Run();

public record TokenRequest(string AccountSid, string ApiKey, string ApiSecret, string TwimlAppSid);

public class CallData
{
    public string CallSid { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string RecordingUrl { get; set; } = "";
    public string AiResponse { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}