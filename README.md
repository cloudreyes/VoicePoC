# VoicePoC - AI Voice Analysis Proof of Concept

A .NET 10.0 Web API and Razor Pages application that integrates **Twilio Voice** and **Google Gemini AI** to record phone calls and automatically generate transcriptions, summaries, and action items.

## 🚀 Overview

This project serves as a proof of concept for:
1.  **Browser-based Calling**: Using the Twilio Voice SDK to initiate calls directly from the browser.
2.  **Call Recording**: Automatically recording both sides of the conversation.
3.  **AI Processing**: Using Google Gemini 2.0 Flash to analyze the audio recording for transcription and insights.

## 🛠️ Prerequisites

To run this project, you will need:
-   **Twilio Account**:
    -   Account SID
    -   API Key and API Secret (Created in the Twilio Console)
    -   A TwiML App SID (Configured with the application's URL for voice webhooks)
    -   A Twilio Phone Number (Verified for outgoing calls)
-   **Google Gemini API Key**:
    -   Access to the Gemini 2.0 Flash model via Google AI Studio.
-   **.NET 10 SDK**: Installed on your development machine.

## ⚙️ Setup & Configuration

### 1. Twilio Configuration
-   Go to the [Twilio Console](https://console.twilio.com/).
-   Create a **TwiML App**:
    -   Set the **Voice Request URL** to: `https://<your-domain>/voice` (Use a tool like [ngrok](https://ngrok.com/) if running locally).
-   Obtain your **Account SID**, **API Key**, and **API Secret**.

### 2. Application Secrets
The application can be configured via `appsettings.json` or Environment Variables.

```json
{
  "TwilioAccountSID": "AC...",
  "TwilioAPIKey": "SK...",
  "TwilioAPISecret": "...",
  "TwilioAppID": "AP...",
  "GeminiAPIKey": "...",
  "FromPhoneNumber": "+1...",
  "ToPhoneNumber": "+1..."
}
```

### 3. Local Development (Optional)
If you want to use User Secrets:
```bash
dotnet user-secrets set "TwilioAccountSID" "AC..."
dotnet user-secrets set "TwilioAPIKey" "SK..."
dotnet user-secrets set "TwilioAPISecret" "..."
dotnet user-secrets set "TwilioAppID" "AP..."
dotnet user-secrets set "GeminiAPIKey" "..."
```

## 🏃 How to Run

1.  **Clone the repository**.
2.  **Configure your settings** (see above).
3.  **Run the application**:
    ```bash
    dotnet run
    ```
4.  **Open the browser** to `http://localhost:5000` (or the configured port).
5.  **Initialize Device**: Enter any missing credentials in the UI and click "Initialize Device".
6.  **Make a Call**: Enter a destination phone number and click "Call Number".
7.  **Analyze**: After the call ends, the system will wait for Twilio to provide the recording, then send it to Gemini for analysis. Results will appear in the "AI Analysis" section.

## 🏗️ Project Structure

-   **Controllers/VoiceController**: Handles API endpoints for Twilio tokens, voice webhooks, and recording callbacks.
-   **Services**:
    -   `ITwilioService`: Generates JWT tokens and TwiML responses.
    -   `IAIService`: Downloads recordings from Twilio and interacts with the Gemini API.
    -   `ICallStorageService`: In-memory storage for tracking call status and AI results.
-   **Models**: Data contracts for API requests and call history.
-   **Pages**: Razor Pages for the front-end UI.

## 📝 License
This project is provided "as is" for demonstration purposes.