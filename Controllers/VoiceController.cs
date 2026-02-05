using Microsoft.AspNetCore.Mvc;
using VoicePoC.Models;
using VoicePoC.Services;

namespace VoicePoC.Controllers;

[ApiController]
[Route("[controller]")]
public class VoiceController(
    ITwilioService twilioService, 
    IAIService aiService, 
    ICallStorageService storageService) : ControllerBase
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

    [HttpGet("/calls")]
    public IActionResult GetCalls()
    {
        return Ok(storageService.GetAllCalls());
    }
}
