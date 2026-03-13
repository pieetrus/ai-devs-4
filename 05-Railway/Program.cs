using AiDevs.Shared;
using _05_Railway.Services;

DotEnv.Load("../.env");

var llm = new LlmClient();
var api = new RailwayApiClient();
var agent = new RailwayAgent(llm, api);

Console.WriteLine("Starting Railway activation agent...");
var flag = await agent.RunAsync();
Console.WriteLine($"\nResult: {flag}");
