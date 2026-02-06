# ADR 0004: Minimal AWS Deployment Architecture (ALB + EC2 + RDS + S3)

## Status
Proposed

## Context
ADR 0003 proposed a high-scale, event-driven architecture (SQS/SNS/Redis/etc.). While powerful, it increases complexity. The current goal is to deploy with minimal application changes, keeping the runtime simple while enabling horizontal scale and basic HA.

Key constraints and goals:
- Keep the application changes minimal.
- Support horizontal scaling behind a load balancer.
- Persist call data in a durable store (Postgres) instead of in-memory.
- Store audio recordings in S3 instead of local/memory.
- Provide basic cross-region resiliency for the database via a read replica.

## Decision
Adopt a minimal AWS infrastructure composed of:
- Application Load Balancer (ALB) as the single public entry point.
- Auto Scaling Group (ASG) of Linux EC2 instances running the web app.
- Amazon RDS for PostgreSQL as the primary data store (single writer in Region A).
- Cross-region read replica of the RDS instance in Region B for disaster recovery.
- Amazon S3 bucket for storing recordings (audio files) and large blobs.

## Architecture Overview
- Traffic (users and webhooks) enters via ALB and is routed to EC2 instances in an ASG.
- The application is stateless. All persistent data is stored in RDS (metadata) and S3 (recordings).
- EC2 instances authenticate to RDS and S3 using IAM roles (instance profile) or secrets where strictly necessary.
- The RDS primary is used for all writes and reads. The cross-region read replica is reserved for DR/failover procedures (not used by the app during normal operation).

## Application Changes (Minimal)
- Replace in-memory call storage with PostgreSQL-backed storage:
  - Add a repository or EF Core DbContext to persist `CallData` records in Postgres.
  - Update `ICallStorageService` implementation to use Postgres instead of the in-memory dictionary.
- Store recordings in S3:
  - Upload/download audio files to/from S3 using the AWS SDK for .NET.
  - Persist only S3 object keys/URLs and related metadata in Postgres.
- Keep the rest of the controller flow and AI integration unchanged.

## Operational Considerations
- Networking: Place EC2 instances in private subnets behind the ALB; RDS in private subnets. Use NAT Gateway for egress if needed.
- IAM: Use instance profiles to grant least-privilege access to S3 (put/get) and, if using IAM auth for RDS, to the database.
- Secrets: If not using IAM auth, store DB credentials securely (e.g., Parameter Store/Secrets Manager). This is optional for this ADR; keep app changes minimal.
- Scaling: Use target-tracking scaling policy on ASG (CPU/requests). RDS instance class sized for expected load; enable storage autoscaling.
- Backups/DR: Enable automated backups and snapshots on RDS. Maintain cross-region read replica; document manual promotion steps for regional failure.
- Observability: Use CloudWatch metrics/logs for EC2/ALB and RDS Performance Insights as needed.

## Consequences
- Pros:
  - Simple path to production with minimal code changes.
  - Stateless compute enables horizontal scaling behind ALB.
  - Durable storage (RDS + S3) improves reliability over in-memory.
  - Cross-region replica provides a DR option without introducing complex event systems.
- Cons:
  - No queue-based buffering; peak loads must be handled by scaling the ASG and RDS.
  - Manual/operational step needed to fail over to the cross-region replica.
  - Some database coupling added vs. pure in-memory.

## Alternatives Considered
- Keep ADR 0003 (SQS/SNS/Redis): Rejected for now due to added complexity; may revisit later.
- Single-region only: Discarded due to DR requirement.

## Rollback
- Revert to the in-memory storage implementation and local file/memory handling of recordings (for development only). For production, prefer staying with RDS/S3 due to durability.

## Links
- Supersedes ADR 0003 (which is now Rejected).
