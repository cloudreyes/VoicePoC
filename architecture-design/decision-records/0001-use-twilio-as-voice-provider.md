# ADR 001: Use Twilio as Voice Provider

## Status
Accepted

## Context
For the VoicePoC, we need a reliable voice provider that allows us to handle incoming calls, record audio, and interact with the user via TwiML.

## Decision
We decided to use Twilio as the primary voice provider for this Proof of Concept.

## Consequences
- **Pros**: 
    - Twilio is well-documented and widely used.
    - It offers a straightforward API and TwiML for call control.
    - Quick setup for a Proof of Concept.
- **Cons**:
    - Potential vendor lock-in if Twilio-specific features are heavily used.
    - Costs associated with Twilio's service.

## Rationale
Twilio was chosen because it represented the path of least resistance for a Proof of Concept. It allowed us to get up and running quickly without complex infrastructure setup.
