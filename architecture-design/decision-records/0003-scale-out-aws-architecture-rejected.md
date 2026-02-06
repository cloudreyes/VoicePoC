# ADR 0003: High-Scale AWS Deployment Architecture

## Status
Rejected - Superseded by ADR 0004

## Context
The current VoicePoC is designed for rapid development with in-memory storage and local configuration. To transition to a production-grade, high-scale, and high-performance system on AWS, we need to address several limitations:
- **Scalability**: Single-instance in-memory storage prevents horizontal scaling.
- **Reliability**: `Task.Run` for AI processing is volatile; if the process crashes, the task is lost.
- **Security**: Local secrets management is not suitable for production.

## Decision
We will adopt a cloud-native architecture on AWS using the following patterns:

1.  **Horizontal Scaling**: Use an **Application Load Balancer (ALB)** and **Auto Scaling Group (ASG)** for EC2 instances.
2.  **Asynchronous Decoupling**: Introduce **AWS SQS** between the `VoiceController` and the `GeminiService`. The controller will push a message to SQS, and a background worker (or the same EC2 instances via a background service) will consume and process it.
3.  **State Management**: Replace in-memory storage with **Amazon RDS (Postgres)** for structured data and **Amazon ElastiCache (Redis)** for real-time state if needed.
4.  **Event-Driven Updates**: Use **AWS SNS** to broadcast "Call Processed" events, allowing multiple downstream consumers (like the RDS update service) to react.
5.  **Secure Configuration**: Use **AWS Secrets Manager** or **AWS AppConfig** to manage sensitive settings, integrated via the AWS Systems Manager Parameter Store provider for .NET.
6.  **Blob Storage**: Store call recordings and AI results in **Amazon S3** for long-term durability and to reduce load on the database.

## Consequences
- **Pros**:
    - High availability and fault tolerance.
    - Ability to handle spikes in call volume via SQS buffering and ASG scaling.
    - Improved security posture using IAM Roles and Secrets Manager.
- **Cons**:
    - Increased architectural complexity.
    - Higher operational cost compared to a single instance.
    - Requires infrastructure-as-code (IaC) management (e.g., Terraform or AWS CDK).
