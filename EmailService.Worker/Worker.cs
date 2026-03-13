using Azure.Messaging.ServiceBus;
using System.Text.Json;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace EmailService.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _queueName = "eagle-email-queue";

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Conecta no Azure buscando a chave do cofre
        string connectionString = _configuration.GetConnectionString("AzureServiceBus");
        
        var client = new ServiceBusClient(connectionString);
        var processor = client.CreateProcessor(_queueName, new ServiceBusProcessorOptions());

        processor.ProcessMessageAsync += MessageHandler;
        processor.ProcessErrorAsync += ErrorHandler;

        _logger.LogInformation("[WORKER] Conectado ao Azure! Iniciando a escuta da fila...");

        await processor.StartProcessingAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("Desligando o Worker...");
        await processor.StopProcessingAsync(stoppingToken);
    }

    // --- FUNÇÃO QUE EXECUTA QUANDO CHEGA MENSAGEM ---
    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        // 1. Pega a mensagem da nuvem e transforma de volta no nosso objeto
        string jsonContent = args.Message.Body.ToString();
        var request = JsonSerializer.Deserialize<EmailRequest>(jsonContent);
        
        _logger.LogInformation($"\n[WORKER] Preparando envio real para: {request.To}");

        // 2. Pega a chave do SendGrid lá do cofre secreto
        string sendGridKey = _configuration["SendGrid:ApiKey"];
        var client = new SendGridClient(sendGridKey);

        // 3. Monta o e-mail
        // ---> ATENÇÃO: MUDE O E-MAIL ABAIXO PARA O SEU E-MAIL VERIFICADO NO SENDGRID <---
        var from = new EmailAddress("pomboamericano308@gmail.com", "Eagle.IA"); 
        var to = new EmailAddress(request.To);
        var msg = MailHelper.CreateSingleEmail(from, to, request.Subject, request.Body, request.Body);

        // 4. Dispara para a internet!
        var response = await client.SendEmailAsync(msg);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("🚀 E-mail REAL enviado com sucesso pela internet!");
            
            // O famoso ACK: Avisa o Azure que deu tudo certo e pode deletar a mensagem
            await args.CompleteMessageAsync(args.Message);
        }
        else
        {
            // Se der erro, NÃO damos o ACK. O Azure tentará novamente depois.
            _logger.LogError($"❌ Falha ao enviar o e-mail. Status: {response.StatusCode}");
        }
    }

    // --- FUNÇÃO QUE EXECUTA SE DER ERRO ---
    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Erro ao processar mensagem do Service Bus.");
        return Task.CompletedTask;
    }
}

// O "Type" (DTO) para o C# saber como ler o JSON que vem da fila
public record EmailRequest(string To, string Subject, string Body);