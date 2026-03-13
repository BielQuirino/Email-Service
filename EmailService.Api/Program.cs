using System.Text.Json;
using Azure.Messaging.ServiceBus;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string connectionString = builder.Configuration.GetConnectionString("AzureServiceBus");
string queueName = "eagle-email-queue";

app.MapPost("/api/emails/send", async (EmailRequest request) =>
{
    await using var client = new ServiceBusClient(connectionString);
    
    ServiceBusSender sender = client.CreateSender(queueName);

    string messageJson = JsonSerializer.Serialize(request);
    ServiceBusMessage message = new ServiceBusMessage(messageJson);

    await sender.SendMessageAsync(message);

    Console.WriteLine($"[API -> AZURE] Message sent to the cloud: {messageJson}");

    return Results.Accepted();
});

app.Run();

public record EmailRequest(string To, string Subject, string Body);
