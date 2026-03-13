# 📧 Async Email Microservice - Eagle.IA 🦅

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Azure Service Bus](https://img.shields.io/badge/Azure_Service_Bus-0078D4?style=for-the-badge&logo=microsoft-azure&logoColor=white)](https://azure.microsoft.com/)
[![SendGrid](https://img.shields.io/badge/SendGrid-009DD9?style=for-the-badge&logo=twilio&logoColor=white)](https://sendgrid.com/)
[![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)

An Event-Driven asynchronous email microservice built with **.NET (C#)**, **Azure Service Bus**, and **SendGrid**. 

Designed for the **Eagle.IA** platform, this service ensures that sending AI-generated meeting summaries does not block the main application thread, providing system scalability, decoupling, and high resilience.

## 🚀 Architecture & Flow

This system solves the classic Background Jobs system design problem by decoupling responsibilities into two distinct applications:

1. **Producer (Web API):** Receives the HTTP `POST` request from the main application, validates the basic data, and packages the message into an **Azure Service Bus** queue. It responds immediately with HTTP `202 Accepted`, freeing up the user's UI.
2. **Consumer (Worker Service):** A background service running 24/7 that listens to the Azure queue. When a message arrives, it consumes it, integrates with the **SendGrid API** for actual delivery, and, upon success, sends an *ACK* (Acknowledgement) to safely remove the message from the queue.

### Why this architecture?
* **Resilience:** If the email provider API (SendGrid) goes down, emails are not lost. They remain safely queued in Azure until the system recovers.
* **Scalability:** We can spin up multiple Worker instances to process thousands of emails concurrently.
* **Decoupling:** The API doesn't know *how* the email is sent, and the Worker doesn't know *who* called the API.

---

## ⚙️ How to Run Locally

### Prerequisites
* [.NET 8 SDK](https://dotnet.microsoft.com/download)
* A free [Azure](https://portal.azure.com/) account (with a Service Bus Namespace and a Queue created).
* A free [SendGrid](https://sendgrid.com/) account (with a verified sender email and an API Key).

### Setup

**1. Clone the repository:**
```bash
git clone [https://github.com/your-username/eagle-email-service.git](https://github.com/your-username/eagle-email-service.git)
cd eagle-email-service
```

**2. Configure Local Secrets (Secret Manager):**
For security reasons, Connection Strings and API Keys are kept out of the source code. You need to configure them locally:

*In the API folder (`EmailService.Api`):*
```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:AzureServiceBus" "YOUR_AZURE_CONNECTION_STRING"
```

*In the Worker folder (`EmailService.Worker`):*
```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:AzureServiceBus" "YOUR_AZURE_CONNECTION_STRING"
dotnet user-secrets set "SendGrid:ApiKey" "YOUR_SENDGRID_API_KEY"
```

**3. Start the Services:**
Open two separate terminals to run the projects concurrently:

**Terminal 1 (API):**
```bash
cd EmailService.Api
dotnet run
```

**Terminal 2 (Worker):**
```bash
cd EmailService.Worker
dotnet run
```

---

## 📡 API Usage

**Endpoint:** `POST /api/emails/send`  
**Content-Type:** `application/json`

**Payload Example:**
```json
{
  "to": "client@example.com",
  "subject": "Meeting Summary - Eagle.IA",
  "body": "Here are the tasks extracted from our planning meeting:\n\n1. Create repository\n2. Deploy database."
}
```

**Expected Responses:**
* `202 Accepted`: Request received, validated, and successfully queued in Azure.
* `400 Bad Request`: Invalid payload format.

  
