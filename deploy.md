# DEPLOYING THE SERVER AND CLIENTS ON SEPARATE PCS

This guide starts the backend (`server`, `rabbitmq`, and `flightReports`) on one PC, the original client on another, and the administrator client on a third. The PCs must be on the same network, the server PC must allow connections through ports 5100 and 5101, and each command must be executed from the application repository root.

## SERVER PC

```bash
cp .env.server.template .env
```

Creates the backend’s private configuration from the template.

```bash
ipconfig getifaddr en0 # macOS
ipconfig # Windows
```

Displays the local IP address that the clients must use.

```bash
nano .env
```

Edit the ports, RabbitMQ credentials, and the IP address of the external user reports server.

```bash
docker compose -f compose.server.yaml up -d rabbitmq
```

Starts RabbitMQ in the background.

The RabbitMQ interface is available at [http://localhost:15672](http://localhost:15672).

```bash
docker compose -f compose.server.yaml up -d --build flight-report-server
```

Builds and starts the first flight reports worker, named `flight-report-worker-1`.

## FLIGHT REPORT WORKERS

The service started alongside the backend operates as the first worker. The following commands create two additional instances of the same consumer.

```bash
docker compose -f compose.server.yaml run -d --rm --name flight-report-worker-2 flight-report-server
```

Starts the second worker in the background.

```bash
docker compose -f compose.server.yaml run -d --rm --name flight-report-worker-3 flight-report-server
```

Starts the third worker in the background.

RabbitMQ delivers each message to a single worker. Setting `prefetchCount` to `1` allows each worker to handle only one pending report at a time.

```bash
docker logs -f flight-report-worker-1
```

Displays the first worker’s logs in a terminal.

```bash
docker logs -f flight-report-worker-2
```

Displays the second worker’s logs in another terminal.

```bash
docker logs -f flight-report-worker-3
```

Displays the third worker’s logs in another terminal.

```bash
docker compose -f compose.server.yaml run --rm --service-ports server
```

Starts the main server interactively and publishes its TCP and WebSocket ports.

## CLIENT PC

```bash
cp .env.client.template .env
```

Creates the client’s private configuration from its template.

```bash
nano .env
```

Set `SERVER_HOST` to the IP address obtained on the server PC and keep `SERVER_PORT` set to `5100`.

```bash
docker compose -f compose.client.yaml build client
```

Builds the original client image.

```bash
docker compose -f compose.client.yaml run --rm client
```

Starts the interactive client and removes its container when it exits.

## ADMINISTRATOR CLIENT PC

```bash
cp .env.client-admin.template .env
```

Creates the administrator client’s private configuration from its template.

```bash
nano .env
```

Set `SERVER_HOST` to the server PC’s IP address and keep ports `5100` and `5101` unchanged.

```bash
docker compose -f compose.client-admin.yaml build client-admin
```

Builds the administrator client image.

```bash
docker compose -f compose.client-admin.yaml run --rm client-admin
```

Starts the interactive administrator client and removes its container when it exits.

## USER REPORTS SERVER PC

This application exposes the gRPC and REST endpoints used by the main server. Its local IP address must be assigned to `USER_REPORT_HOST` in the `.env` file on the main server PC.

```bash
cp .env.user-report.template .env
```

Creates the user reports server’s private configuration.

```bash
ipconfig getifaddr en0 # macOS
ipconfig # Windows
```

Displays the local IP address that the main server must use.

```bash
docker compose -f compose.user-report.yaml up -d --build user-report-server
```

Builds and starts the user reports server in the background.
