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
        string connectionString = _configuration.GetConnectionString("AzureServiceBus");
        
        var client = new ServiceBusClient(connectionString);
        var processor = client.CreateProcessor(_queueName, new ServiceBusProcessorOptions());

        processor.ProcessMessageAsync += MessageHandler;
        processor.ProcessErrorAsync += ErrorHandler;

        _logger.LogInformation("[WORKER] Connected to Azure! Starting to listen to the queue...");

        await processor.StartProcessingAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("Shutting down the worker...");
        await processor.StopProcessingAsync(stoppingToken);
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        string jsonContent = args.Message.Body.ToString();
        var request = JsonSerializer.Deserialize<EmailRequest>(jsonContent);
        
        _logger.LogInformation($"\n[WORKER] Preparing to send to: {request.To}");

        string sendGridKey = _configuration["SendGrid:ApiKey"];
        var client = new SendGridClient(sendGridKey);

        var from = new EmailAddress("registeredEmail@test.com", "Eagle.IA"); 
        var to = new EmailAddress(request.To);
        var msg = MailHelper.CreateSingleEmail(from, to, request.Subject, request.Body, request.Body);

        var response = await client.SendEmailAsync(msg);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Email sent successfully.");
            
            await args.CompleteMessageAsync(args.Message);
        }
        else
        {
            _logger.LogError($"Failed to send email. Status: {response.StatusCode}");
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Error processing Service Bus message.");
        return Task.CompletedTask;
    }
}

public record EmailRequest(string To, string Subject, string Body);
