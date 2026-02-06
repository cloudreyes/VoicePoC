# ADR 002: Use In-Memory Storage for Call Data

## Status
Accepted

## Context
The application needs to store call metadata and state during and after the call lifecycle for the dashboard and processing.

## Decision
We decided to use an in-memory storage implementation (`CallStorageService`) instead of a persistent database.

## Consequences
- **Pros**:
    - Zero configuration required.
    - Extremely fast development and execution.
    - No external dependencies (SQL Server, Redis, etc.) needed for the PoC.
- **Cons**:
    - Data is lost whenever the application restarts.
    - Not suitable for production or multi-instance deployments.

## Rationale
In-memory storage was chosen because it was the path of least resistance for this Proof of Concept. The goal was to demonstrate functionality rather than persistence architecture.
