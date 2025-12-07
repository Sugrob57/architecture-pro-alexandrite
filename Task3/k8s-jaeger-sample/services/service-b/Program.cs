using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ConcurrentBag<Order>>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("MES"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation();
        t.AddHttpClientInstrumentation();
        t.AddSource("MES");
        t.AddOtlpExporter(exporter =>
        {
            exporter.Endpoint = new Uri("http://simplest-collector.observability.svc.cluster.local:4317");
        });
    });

var app = builder.Build();
Console.WriteLine($"app started");

var storage = new ConcurrentBag<Order>();

app.MapPost("/order", (Order order, ConcurrentBag<Order> storage) =>
{
    try
    {
        Console.WriteLine($"write order {order.Id}");
        order.Price = order.Quantity * 10 + 5; // имитация расчёта
	    storage.Add(order);
	    return Results.Ok(order);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"write order failed {ex}");
        throw;
    }
});

app.MapGet("/order", (ConcurrentBag<Order> storage) =>
{
    return Results.Ok(storage);
});

app.Run("http://0.0.0.0:8082");

class Order()
{
	public int Id { get; set; }

	public int Quantity { get; set; }

	public decimal Price { get; set; }
}
