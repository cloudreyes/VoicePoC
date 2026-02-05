namespace VoicePoC.Models;

public record TokenRequest(string AccountSid, string ApiKey, string ApiSecret, string TwimlAppSid);

public class CallData
{
    public string CallSid { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string RecordingUrl { get; set; } = "";
    public string AiResponse { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
