using Twilio.Jwt.AccessToken;
using Twilio.TwiML;
using Twilio.TwiML.Voice;
using VoicePoC.Models;

namespace VoicePoC.Services;

public interface ITwilioService
{
    string GenerateToken(TokenRequest req);
    string GenerateVoiceResponse(IFormCollection form);
}

public class TwilioService : ITwilioService
{
    public string GenerateToken(TokenRequest req)
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

        return token.ToJwt();
    }

    public string GenerateVoiceResponse(IFormCollection form)
    {
        var response = new VoiceResponse();

        var numberToCall = form.ContainsKey("To") ? form["To"].ToString() : "";
        var callerId = form.ContainsKey("CallerId") ? form["CallerId"].ToString() : "";
        var geminiKey = form.ContainsKey("GeminiKey") ? form["GeminiKey"].ToString() : "";
        var apiKey = form.ContainsKey("TwilioApiKey") ? form["TwilioApiKey"].ToString() : "";
        var apiSecret = form.ContainsKey("TwilioApiSecret") ? form["TwilioApiSecret"].ToString() : "";

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

        return response.ToString();
    }
}
