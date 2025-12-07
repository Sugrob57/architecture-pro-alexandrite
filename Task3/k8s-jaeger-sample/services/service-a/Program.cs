using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// HttpClient → MES
builder.Services.AddHttpClient("mes", client =>
{
});

builder.Services.AddOpenTelemetry()
	.ConfigureResource(r => r.AddService("InternetShop"))
	.WithTracing(t =>
	{
		t.AddAspNetCoreInstrumentation();
		t.AddHttpClientInstrumentation();
		t.AddSource("InternetShop");
		t.AddOtlpExporter(exporter =>
		{
			exporter.Endpoint = new Uri("http://simplest-collector.observability.svc.cluster.local:4317");
		});
	});

var app = builder.Build();
Console.WriteLine("app started");
app.MapPost("/order", async (Order order, IHttpClientFactory httpFactory) =>
{
	try
	{
       Console.WriteLine($"create order {order.Id}");
		var http = httpFactory.CreateClient("mes");

		var response = await http.PostAsJsonAsync("http://service-b:8082/order", order);
		var calculated = await response.Content.ReadFromJsonAsync<Order>();

		return Results.Ok(calculated);
	}
	catch (Exception ex)
	{
		Console.WriteLine($"create order failed {ex}");
		throw;
	}
});

app.Run("http://0.0.0.0:8081");

class Order()
{
	public int Id { get; set; }

	public int Quantity { get; set; }

	public decimal Price { get; set; }
}