using System.Text.Json;
using Azure.Messaging.ServiceBus;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 1. Sua chave de acesso do Azure e o nome da fila
// O .NET vai buscar a chave no cofre do seu computador automaticamente
string connectionString = builder.Configuration.GetConnectionString("AzureServiceBus");
string queueName = "eagle-email-queue";

app.MapPost("/api/emails/send", async (EmailRequest request) =>
{
    // 2. Cria a conexão com a nuvem da Microsoft
    await using var client = new ServiceBusClient(connectionString);
    
    // 3. Cria o "remetente" apontando para a nossa fila específica
    ServiceBusSender sender = client.CreateSender(queueName);

    // 4. Transforma o pedido em JSON e empacota no formato do Service Bus
    string mensagemJson = JsonSerializer.Serialize(request);
    ServiceBusMessage message = new ServiceBusMessage(mensagemJson);

    // 5. Envia para a nuvem!
    await sender.SendMessageAsync(message);

    Console.WriteLine($"[API -> AZURE] Mensagem enviada para a nuvem: {mensagemJson}");

    // 6. Libera a aplicação principal rapidamente
    return Results.Accepted();
});

app.Run();

public record EmailRequest(string To, string Subject, string Body);