using Blackfinch.Lending.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddLendingPlatform();

using var host = builder.Build();
await host.RunAsync();
