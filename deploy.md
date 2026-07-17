DESPLIEGUE DEL SERVIDOR Y LOS CLIENTES EN PCS SEPARADAS

Esta guía inicia el backend (server, rabbitmq y flightReports) en una PC, el cliente original en otra y el cliente administrador en una tercera. Las PCs deben estar en la misma red, la PC del servidor debe aceptar conexiones por los puertos 5100 y 5101, y cada comando debe ejecutarse desde la raíz del repositorio de la aplicación.

PC DEL SERVIDOR

```bash
cp .env.server.template .env
```

Crea la configuración privada del backend a partir de la plantilla.

```bash
ipconfig getifaddr en0 #mac
ipconfig #windows
```

Muestra la IP local que deberá usar el cliente.

```bash
nano .env
```

Edita los puertos, credenciales de RabbitMQ y la IP del servidor externo de reportes de usuarios.

```bash
docker compose -f compose.server.yaml up -d rabbitmq
```

Inicia RabbitMQ en segundo plano.
La interfaz de RabbitMQ queda disponible en [http://localhost:15672](http://localhost:15672).

```bash
docker compose -f compose.server.yaml up -d --build flight-report-server
```

Construye e inicia el primer worker de reportes de vuelos con el nombre flight-report-worker-1.

WORKERS DE REPORTES DE VUELOS
El servicio iniciado con el backend funciona como el primer worker. Los siguientes comandos crean dos instancias adicionales del mismo consumidor.

```bash
docker compose -f compose.server.yaml run -d --rm --name flight-report-worker-2 flight-report-server
```

Inicia el segundo worker en segundo plano.

```bash
docker compose -f compose.server.yaml run -d --rm --name flight-report-worker-3 flight-report-server
```

Inicia el tercer worker en segundo plano.

RabbitMQ entrega cada mensaje a un solo worker. El valor prefetchCount en 1 permite que cada worker mantenga un único reporte pendiente.

```bash
docker logs -f flight-report-worker-1
```

Muestra los logs del primer worker en una terminal.

```bash
docker logs -f flight-report-worker-2
```

Muestra los logs del segundo worker en otra terminal.

```bash
docker logs -f flight-report-worker-3
```

Muestra los logs del tercer worker en otra terminal.

```bash
docker compose -f compose.server.yaml run --rm --service-ports server
```

Inicia el servidor principal de forma interactiva y publica sus puertos TCP y WebSocket.

PC DEL CLIENTE

```bash
cp .env.client.template .env
```

Crea la configuración privada del cliente a partir de su plantilla.

```bash
nano .env
```

Asigna a SERVER_HOST la IP obtenida en la PC del servidor y conservar SERVER_PORT en 5100.

```bash
docker compose -f compose.client.yaml build client
```

Construye la imagen del cliente original.

```bash
docker compose -f compose.client.yaml run --rm client
```

Inicia el cliente interactivo y elimina su contenedor cuando finaliza.

PC DEL CLIENTE ADMINISTRADOR

```bash
cp .env.client-admin.template .env
```

Crea la configuración privada del cliente administrador a partir de su template.

```bash
nano .env
```

Asigna a SERVER_HOST la IP de la PC del servidor y conserva los puertos 5100 y 5101.

```bash
docker compose -f compose.client-admin.yaml build client-admin
```

Construye la imagen del cliente administrador.

```bash
docker compose -f compose.client-admin.yaml run --rm client-admin
```

Inicia el cliente administrador interactivo y elimina su contenedor cuando finaliza.

PC DEL SERVIDOR DE REPORTES DE USUARIOS

Esta aplicación expone los endpoints gRPC y REST usados por el servidor principal. Su IP local debe asignarse a USER_REPORT_HOST en el archivo .env de la PC del servidor principal.

```bash
cp .env.user-report.template .env
```

Crea la configuración privada del servidor de reportes de usuarios.

```bash
ipconfig getifaddr en0 #mac
ipconfig #windows
```

Muestra la IP local que debe usar el servidor principal.

```bash
docker compose -f compose.user-report.yaml up -d --build user-report-server
```

Construye e inicia el servidor de reportes de usuarios en segundo plano.
