# 0002.Code design and architecture

* **Status**: Proposed
* **Data**: 29-09-2026

## Decision

1. Each module will have its own database schema.
2. The versioning of the database schema will be managed using FluentMigrator.

3. The modules will be independent and loosely coupled.
4. The modules will communicate with each other through well-defined and versioned interfaces.
5. Outbox pattern will be used to ensure reliable communication between modules.
6. The platform will guarantee race condition and deadlock prevention by using optimistic concurrency control and proper transaction management.

7. All platform messages will follow versioning strategy that allows app backward compatibility using canary releases and feature flags.
8. Error responses will follow RFC 7807 - Problem Details for HTTP APIs standard.