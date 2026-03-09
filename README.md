# AI Devs 4

Solutions to [AI Devs 4](https://aidevs.pl) tasks in .NET 10 / C#.

## Structure

```
AiDevs.Shared/          shared library used by all tasks
  LlmClient.cs          OpenRouter-backed chat client
  AiDevsClient.cs       posts answers to hub.ag3nts.org/verify
```

## Setup

Copy `env.example` to `.env` (or export vars in your shell):

```
OPENROUTER_API_KEY=sk-or-...
AI_DEVS_API_KEY=your-key
```

## Running a task

```bash
cd 01-PeopleTask
dotnet run
```

## Shared library

`AiDevs.Shared` handles env vars and HTTP plumbing so task code stays focused:

```csharp
// LLM call
var chat = new LlmClient().GetChatClient("openai/gpt-4o-mini");

// Submit answer
var result = await new AiDevsClient().VerifyAsync("task-name", answer);
```
