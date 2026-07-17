# Containerized Distributed Airline System

- **Docker and Docker Compose:** Package the server, clients, report services, and RabbitMQ into reproducible containers.
- **Multistage builds and cache:** The server compiles with the .NET SDK and runs on the smaller ASP.NET runtime image, reducing the final image by approximately **75%** copying project files before source files also preserves Docker’s dependency cache.
- **TCP:** Custom binary protocol used by clients for authentication, flights, tickets, and images.
- **WebSockets:** Sends live ticket-purchase notifications to the administrator client.
- **MOM - RabbitMQ:** Queues flight-cancellation report jobs so multiple workers can process them asynchronously.
- **gRPC and RESTful api:** Connect the main server with the user-report service and expose completed reports.
- **Protocol Buffers:** Defines the gRPC contracts.
- **PuppeteerSharp and Chromium:** Generate PDF reports.
- **Deployment:** See [deploy.md](deploy.md) for the complete setup guide.
