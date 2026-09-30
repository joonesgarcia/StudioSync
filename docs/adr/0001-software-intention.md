# 0001.Software Intention and design

* **Status**: Proposed
* **Data**: 29-09-2026

## Context
The platform aims to provide a comprehensive solution for managing bookings, capacity, and waitlists for studios. 
The system will be designed to handle high traffic and provide a seamless user experience.

## Decision
We have decided to implement a modular monolith architecture for the platform. 

This decision was made after considering the trade-offs, maintainability, and scalability of the system. 
The modular monolith architecture allows for better organization of code, easier refactoring, and the ability to scale individual modules as needed.

This is the proposed roadmap:

[ Milestone 1 ]  Relational Modeling & Schema Migrations (PostgreSQL/SQL Server + FluentMigrator)
       │
[ Milestone 2 ]  Clean API Architecture & CQRS (C# / .NET 9 + GraphQL / REST)
       │
[ Milestone 3 ]  Event-Driven Processing & Resiliency (AWS SQS/SNS, Redis, DynamoDB, Outbox Pattern)
       │
[ Milestone 4 ]  Full-Stack & Observability (TypeScript/Next.js + NLog/CloudWatch)

## Options Considered

* **Option 1:** Starting small with a monolithic architecture and gradually refactoring to microservices as the system grows.
* **Option 2:** Starting with a modular monolith architecture, allowing for easier refactoring and scaling in the future.
* **Option 3:** Starting with a microservices architecture from the beginning, allowing for better scalability and flexibility, but with higher complexity and overhead.

## Consequences

The consequences of this decision are that we will have a more organized codebase, easier refactoring, and the ability to scale individual modules as needed. 
However, we may face challenges in terms of deployment and scaling as the system grows.

### Positive

Benefit 1: Reduced complexity and overhead compared to a microservices architecture, allowing for faster development and deployment.
Benefit 2: Ability to scale individual modules as needed, allowing for better performance and resource utilization

### Negative (Trade-offs / Riscos)

Negative 1: Potential for tight coupling between modules, making it harder to change or replace individual modules in the future.