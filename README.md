# KoreForge.Json

JSON utilities for the KoreForge ecosystem.

## Components

- **KF.Json** — Core JSON utilities with zero KoreForge dependencies
  - `RootPropertyClassifier` — UTF-8 zero-allocation early routing for Kafka messages
  - `JsonMaterializer` — Recursive expansion of escaped JSON string values
- **KF.Json.Jex** — JEX integration bridge (registers `expandJson()` function)

## Installation

```
dotnet add package KoreForge.Json
```

## Quick Start

### RootPropertyClassifier

Build once at startup, call per-message with raw `byte[]`:

```csharp
var classifier = new RootPropertyClassifier(
    propertyNames: ["Action", "MessageType"],
    matches:
    [
        new(10, new Regex("^OrderCreated$", RegexOptions.Compiled)),
        new(20, new Regex("^OrderUpdated$", RegexOptions.Compiled)),
    ]);

int result = classifier.Classify(messageBytes);
```

### JsonMaterializer

Recursively expand escaped JSON strings in a document:

```csharp
var result = JsonMaterializer.Expand(token, new JsonMaterializerOptions
{
    MaxDepth   = 10,
    FieldHints = ["Data", "Payload"]
});
```
