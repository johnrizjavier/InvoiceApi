# Invoice API

A production-ready Invoice/Billing REST API built with **Azure Functions** (.NET 8) featuring enterprise-grade integrations with **Stripe**, **SendGrid**, and **Twilio**.

![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Azure Functions](https://img.shields.io/badge/Azure%20Functions-v4-blue)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **Invoice Management** - Create, read, update, and delete invoices with line items
- **Stripe Payments** - Generate payment links and handle webhook events
- **Email Notifications** - Send invoices and confirmations via SendGrid
- **SMS Notifications** - Payment reminders via Twilio
- **API Key Authentication** - Secure your endpoints
- **Azure CosmosDB** - Scalable NoSQL data storage
- **CI/CD Pipeline** - GitHub Actions for automated deployment

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Azure Functions                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │   Invoices   │  │   Payments   │  │    Notifications     │  │
│  │    CRUD      │  │   (Stripe)   │  │ (SendGrid + Twilio)  │  │
│  └──────────────┘  └──────────────┘  └──────────────────────┘  │
│                              │                                   │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │                    API Key Middleware                       │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                               │
        ┌──────────────────────┼──────────────────────┐
        ▼                      ▼                      ▼
┌───────────────┐     ┌───────────────┐      ┌───────────────┐
│  Azure        │     │    Stripe     │      │   SendGrid    │
│  CosmosDB     │     │   Payments    │      │   + Twilio    │
└───────────────┘     └───────────────┘      └───────────────┘
```

## API Endpoints

### Invoices

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/invoices` | Create a new invoice |
| `GET` | `/api/invoices` | List all invoices (with filters) |
| `GET` | `/api/invoices/{id}` | Get invoice by ID |
| `PUT` | `/api/invoices/{id}` | Update an invoice |
| `DELETE` | `/api/invoices/{id}` | Delete an invoice |
| `POST` | `/api/invoices/{id}/send` | Send invoice via email |
| `POST` | `/api/invoices/{id}/remind` | Send SMS payment reminder |

### Payments

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/invoices/{id}/payment-link` | Generate Stripe payment link |
| `POST` | `/api/webhooks/stripe` | Handle Stripe webhooks |

### Health

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/health` | Health check (no auth required) |

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [Azure CosmosDB Emulator](https://docs.microsoft.com/azure/cosmos-db/local-emulator) (for local development)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/InvoiceApi.git
   cd InvoiceApi
   ```

2. **Configure settings**

   Copy `local.settings.json` in `InvoiceApi.Functions` and update with your credentials:
   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",

       "CosmosDb:Endpoint": "https://localhost:8081",
       "CosmosDb:Key": "your-cosmos-key",
       "CosmosDb:DatabaseName": "InvoiceDb",
       "CosmosDb:ContainerName": "Invoices",

       "Stripe:SecretKey": "sk_test_...",
       "Stripe:WebhookSecret": "whsec_...",

       "SendGrid:ApiKey": "SG...",
       "SendGrid:FromEmail": "invoices@yourdomain.com",

       "Twilio:AccountSid": "AC...",
       "Twilio:AuthToken": "your-auth-token",
       "Twilio:FromNumber": "+1234567890",

       "ApiKey": "your-secure-api-key"
     }
   }
   ```

3. **Run locally**
   ```bash
   cd InvoiceApi.Functions
   func start
   ```

## API Usage Examples

### Create an Invoice

```bash
curl -X POST http://localhost:7071/api/invoices \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{
    "client": {
      "name": "John Doe",
      "email": "john@example.com",
      "phone": "+1234567890",
      "company": "Acme Inc"
    },
    "lineItems": [
      {
        "description": "Web Development Services",
        "quantity": 40,
        "unitPrice": 150.00
      },
      {
        "description": "UI/UX Design",
        "quantity": 20,
        "unitPrice": 125.00
      }
    ],
    "taxRate": 8.5,
    "dueDate": "2025-01-15T00:00:00Z",
    "notes": "Payment due within 30 days"
  }'
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "abc123",
    "invoiceNumber": "INV-20241209-4521",
    "client": {
      "name": "John Doe",
      "email": "john@example.com"
    },
    "lineItems": [...],
    "subtotal": 8500.00,
    "taxRate": 8.5,
    "taxAmount": 722.50,
    "total": 9222.50,
    "status": "Draft",
    "issueDate": "2024-12-09T10:30:00Z",
    "dueDate": "2025-01-15T00:00:00Z"
  },
  "message": "Invoice created successfully"
}
```

### Generate Payment Link

```bash
curl -X POST http://localhost:7071/api/invoices/abc123/payment-link \
  -H "X-API-Key: your-api-key"
```

**Response:**
```json
{
  "success": true,
  "paymentLinkUrl": "https://checkout.stripe.com/pay/cs_test_...",
  "paymentLinkId": "cs_test_..."
}
```

### Send Invoice via Email

```bash
curl -X POST http://localhost:7071/api/invoices/abc123/send \
  -H "X-API-Key: your-api-key"
```

### List Invoices with Filters

```bash
curl "http://localhost:7071/api/invoices?status=Sent&fromDate=2024-01-01&take=10" \
  -H "X-API-Key: your-api-key"
```

## Project Structure

```
InvoiceApi/
├── InvoiceApi.Functions/        # Azure Functions (HTTP endpoints)
│   ├── Functions/               # Function implementations
│   ├── Middleware/              # API Key authentication
│   └── Program.cs               # Application entry point
│
├── InvoiceApi.Core/             # Domain layer
│   ├── Entities/                # Invoice, Client, LineItem, Payment
│   ├── DTOs/                    # Request/Response objects
│   ├── Interfaces/              # Repository & service contracts
│   └── Services/                # Business logic
│
├── InvoiceApi.Infrastructure/   # External integrations
│   ├── Persistence/             # CosmosDB repository
│   ├── Payments/                # Stripe integration
│   └── Notifications/           # SendGrid & Twilio
│
└── InvoiceApi.Tests/            # Unit & integration tests
```

## Deployment

### Azure Portal

1. Create an Azure Function App (Consumption or Premium plan)
2. Create an Azure CosmosDB account
3. Configure application settings with your credentials
4. Deploy using GitHub Actions or Azure CLI

### GitHub Actions (Recommended)

1. Get your Function App publish profile from Azure Portal
2. Add it as a secret `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` in your repo
3. Update `AZURE_FUNCTIONAPP_NAME` in `.github/workflows/deploy.yml`
4. Push to `main` branch to trigger deployment

## Third-Party Services Setup

### Stripe

1. Create a [Stripe account](https://stripe.com)
2. Get your API keys from the Dashboard
3. Set up webhook endpoint: `https://your-function-app.azurewebsites.net/api/webhooks/stripe`
4. Add `checkout.session.completed` event

### SendGrid

1. Create a [SendGrid account](https://sendgrid.com)
2. Create an API key with "Mail Send" permission
3. Verify your sender email address

### Twilio

1. Create a [Twilio account](https://twilio.com)
2. Get a phone number for SMS
3. Note your Account SID and Auth Token

## Environment Variables

| Variable | Description |
|----------|-------------|
| `CosmosDb:Endpoint` | CosmosDB account endpoint URL |
| `CosmosDb:Key` | CosmosDB access key |
| `CosmosDb:DatabaseName` | Database name (default: InvoiceDb) |
| `CosmosDb:ContainerName` | Container name (default: Invoices) |
| `Stripe:SecretKey` | Stripe secret API key |
| `Stripe:WebhookSecret` | Stripe webhook signing secret |
| `SendGrid:ApiKey` | SendGrid API key |
| `SendGrid:FromEmail` | Sender email address |
| `Twilio:AccountSid` | Twilio Account SID |
| `Twilio:AuthToken` | Twilio Auth Token |
| `Twilio:FromNumber` | Twilio phone number |
| `ApiKey` | Your API key for authentication |

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## License

MIT License - feel free to use this project for your own purposes.

## Author

Built with Azure Functions, .NET 8, and modern cloud integrations.

---

**Questions?** Feel free to open an issue or reach out!
