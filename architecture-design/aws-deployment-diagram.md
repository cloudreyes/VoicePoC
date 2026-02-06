# AWS Target Architecture Diagram (Minimal)

This diagram illustrates the proposed minimal AWS deployment for VoicePoC per ADR 0004.

```mermaid
graph TD
    subgraph "External"
        PSTN[Phone / Twilio]
        Users[Web Users]
    end

    subgraph "AWS Cloud (Region A)"
        ALB[Application Load Balancer]
        
        subgraph "Auto Scaling Group (EC2)"
            Web[Web App Instances (Linux)]
        end

        S3[Amazon S3 - Recordings]
        RDS[(Amazon RDS - Postgres Primary)]
    end

    subgraph "AWS Cloud (Region B)"
        RDSReplica[(RDS Postgres Read Replica)]
    end

    subgraph "AI / 3rd Party"
        Gemini[Google Gemini API]
        Twilio[Twilio API]
    end

    %% Flow
    Users -- "HTTPS" --> ALB
    PSTN -- "Webhooks" --> ALB
    ALB -- "Route Requests" --> Web
    
    Web -- "Download Recording" --> Twilio
    Web -- "Analyze" --> Gemini
    Web -- "Store Recording" --> S3
    
    Web -- "Read/Write Metadata" --> RDS
    RDS -- "Logical Replication" --> RDSReplica
```

### Notes
- Minimal changes to the app: replace in-memory storage with Postgres and store recordings in S3.
- No SQS/SNS/Redis at this stage; can be introduced later if needed.
- Cross-region read replica is for DR; application uses the primary RDS in normal operation.
