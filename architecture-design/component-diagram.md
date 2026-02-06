# Application Components Diagram

This diagram provides a high-level overview of the VoicePoC architecture, illustrating how the various components interact to provide browser-based calling and AI-powered call analysis.

```mermaid
graph TD
    subgraph "Client Side (Browser)"
        UI[Razor Pages UI]
        SDK[Twilio Voice JS SDK]
    end

    subgraph "Server Side (ASP.NET Core)"
        VC[VoiceController]
        TS[TwilioService]
        GS[GeminiService]
        CSS[CallStorageService - In-Memory]
    end

    subgraph "External Services"
        T_API[Twilio Cloud API]
        G_API[Google Gemini AI API]
    end

    %% Interactions
    UI <--> VC
    UI --> SDK
    SDK <--> T_API
    
    VC --> TS
    VC --> CSS
    VC -- "Fire & Forget" --> GS
    
    TS <--> T_API
    GS --> T_API : "Download Recording"
    GS --> G_API : "Analyze Audio"
    GS --> CSS : "Update Results"
    
    T_API -- "Webhooks" --> VC
```

### Component Descriptions

*   **Razor Pages UI**: The front-end of the application where users initiate calls and view AI analysis results.
*   **Twilio Voice JS SDK**: Enables the browser to act as a softphone, handling the audio stream directly with Twilio.
*   **VoiceController**: The main API entry point. It handles token generation for the client, responds to Twilio webhooks (voice and recording status), and provides call history.
*   **TwilioService**: Encapsulates logic for generating Twilio Access Tokens and TwiML instructions.
*   **GeminiService**: Responsible for downloading the call recording from Twilio, processing it through Google Gemini AI, and extracting transcriptions and insights.
*   **CallStorageService**: A temporary, in-memory store for call metadata and analysis results (chosen for PoC simplicity).
*   **Twilio Cloud API**: Managed service for PSTN connectivity, call routing, and recording storage.
*   **Google Gemini AI API**: Multi-modal AI model used for high-quality audio transcription and summarization.
