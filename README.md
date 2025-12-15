# ShoppingAssistant

An Azure Functions (v4) timer-triggered application that monitors product availability on specific web store and sends notifications when products become available.

## How It Works

1. A timer trigger runs daily at 07:00 UTC
2. Fetches HTML from a configured list of product URLs
3. Parses each page using HtmlAgilityPack to check if the product is sold out (looks for `sold-out` class or disabled add-to-cart buttons)
4. If any products are available, calls a Logic App to send notifications grouped by country

## Tech Stack

- .NET 8
- Azure Functions v4 (isolated worker)
- HtmlAgilityPack for HTML parsing
- Newtonsoft.Json for JSON serialization
- SendGrid (email integration)
- Azure Logic App (notification workflow)

## Configuration

The following settings are required in `local.settings.json` (local) or Application Settings (Azure):

| Setting | Description |
|---|---|
| `UrlList` | Comma-separated list of product page URLs to monitor |
| `LogicAppUrl` | HTTP trigger URL for the Logic App that sends notifications |

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)

### Run Locally

```bash
cd ShoppingAssistant
cp local.settings.json.example local.settings.json  # configure your settings
func start
```

### Build

```bash
dotnet build
```
