using Microsoft.AspNetCore.Mvc;
using VoicePoC.Models;
using VoicePoC.Services;

namespace VoicePoC.Controllers;

[ApiController]
[Route("[controller]")]
public class VoiceController(
    ITwilioService twilioService, 
    IAIService aiService, 
    ICallStorageService storageService,
    IHttpClientFactory clientFactory) : ControllerBase
{
    [HttpPost("/token")]
    public IActionResult GetToken([FromBody] TokenRequest req)
    {
        var token = twilioService.GenerateToken(req);
        return Ok(new { token });
    }

    [HttpPost("/voice")]
    [Consumes("application/x-www-form-urlencoded")]
    public IActionResult VoiceWebhook()
    {
        var response = twilioService.GenerateVoiceResponse(Request.Form);
        return Content(response, "application/xml");
    }

    [HttpPost("/recording-completed")]
    [Consumes("application/x-www-form-urlencoded")]
    public IActionResult RecordingCompleted()
    {
        var recordingUrl = Request.Form["RecordingUrl"].ToString();
        var callSid = Request.Form["CallSid"].ToString();

        var geminiKey = Request.Query["k"].ToString();
        var apiKey = Request.Query["ak"].ToString();
        var apiSecret = Request.Query["as"].ToString();

        if (string.IsNullOrEmpty(recordingUrl)) return Ok();

        var data = new CallData 
        { 
            CallSid = callSid, 
            Status = "Processing AI...", 
            RecordingUrl = recordingUrl 
        };
        storageService.UpsertCall(callSid, data);

        // Fire and forget AI processing
        _ = Task.Run(async () =>
        {
            await aiService.ProcessRecordingAsync(recordingUrl, callSid, geminiKey, apiKey, apiSecret, data);
            storageService.UpsertCall(callSid, data);
        });

        return Ok();
    }

    [HttpGet("/recording-proxy")]
    public async Task<IActionResult> ProxyRecording([FromQuery] string url, [FromQuery] string ak, [FromQuery] string @as)
    {
        if (string.IsNullOrEmpty(url)) return BadRequest("URL is required");

        var httpClient = clientFactory.CreateClient();
        var authToken = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{ak}:{@as}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

        var audioUrl = url.EndsWith(".mp3") ? url : url + ".mp3";
        var response = await httpClient.GetAsync(audioUrl, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "audio/mpeg";
        var stream = await response.Content.ReadAsStreamAsync();

        return File(stream, contentType);
    }

    [HttpGet("/calls")]
    public IActionResult GetCalls()
    {
        return Ok(storageService.GetAllCalls());
    }
}
