

# YouxelStorageTask

## Overview

YouxelStorageTask is a microservice designed to handle file storage operations using multiple cloud storage providers, such as AWS S3 and Azure Blob Storage. It supports file upload, retrieval, and deletion, and communicates with other services via RabbitMQ for asynchronous operations.

## Features

- **Multi-Cloud Support**: Integrates with AWS S3 and Azure Blob Storage.
- **Asynchronous Processing**: Uses RabbitMQ for handling file operations asynchronously.
- **Swagger Documentation**: Provides interactive API documentation for ease of use and testing.
- **Logging**: Utilizes Serilog for structured logging and log management.

## Getting Started

### Prerequisites

- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) 
- [Docker](https://www.docker.com/products/docker-desktop) (optional, for containerization)
- [RabbitMQ](https://www.rabbitmq.com/download.html) (for local development)
- An AWS or Azure account for cloud storage configuration

### Configuration

#### AppSettings

Configure your settings in `appsettings.json`:

```json
{
  "StorageProvider": "Azure", // Change this to "AWS" to switch to AWS or "Azure" to switch to Azure
  "Azure": {
    "BlobStorageConnectionString": "your-azure-connection-string",
    "ContainerName": "your-azure-container-name"
  },
  "AWS": {
    "AccessKey": "your-aws-access-key",
    "SecretKey": "your-aws-secret-key",
    "BucketName": "your-aws-bucket-name",
    "Region": "your-Region"
  },
  "RabbitMQ": {
    "HostName": "your-HostName",
    "UserName": "your-UserName",
    "Password": "your-Password",
    "RequestQueueName": "Storage_Request_Queue",
    "ResponseQueueName": "Storage_Response_Queue"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}

```

Replace placeholders with your actual configuration values.

### Installation

1. **Clone the Repository**

   ```bash
   git clone https://github.com/manarbishr/YouxelStorageTask.git
   cd YouxelStorageTask
   ```

2. **Restore Dependencies**

   ```bash
   dotnet restore
   ```

3. **Build the Project**

   ```bash
   dotnet build
   ```

4. **Run the Application**

   ```bash
   dotnet run
   ```

   The service will be available at `http://localhost:5000` (default port).

### API Documentation

- **Swagger UI**: Access interactive API documentation at `http://localhost:5000/swagger`.

### Usage

#### Upload File

**Endpoint**: `POST /api/files/upload`

**Request**: Multipart/form-data with file

**Response**: JSON object with file metadata

#### Get File

**Endpoint**: `GET /api/files/{fileId}`

**Response**: File content

#### Delete File

**Endpoint**: `DELETE /api/files/{fileId}`

**Response**: Confirmation message

### Testing

Unit tests are included in the `YouxelStorageTask.Tests` project. Run the tests with:

```bash
dotnet test
```
