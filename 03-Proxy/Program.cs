using _03_Proxy;
using _03_Proxy.Services;
using AiDevs.Shared;

DotEnv.Load();
DotEnv.Load("../.env");

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<LlmClient>();
builder.Services.AddSingleton<PackageApiClient>();
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddSingleton<ProxyAgent>();

var app = builder.Build();

app.MapPost("/", async (OperatorRequest request, SessionStore sessions, ProxyAgent agent) =>
{
    Console.WriteLine($">>> [{request.SessionId}] {request.Msg}");
    var history = sessions.GetOrCreate(request.SessionId);
    var response = await agent.RunAsync(history, request.Msg);
    Console.WriteLine($"<<< [{request.SessionId}] {response}");
    return Results.Json(new OperatorResponse(response));
});

app.Run("http://0.0.0.0:3000");
