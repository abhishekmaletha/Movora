# Docker Compose Project

This project sets up a multi-container Docker application using Docker Compose. It includes the following services:

- **Movora.WebAPI**: A web API service built with .NET.
- **Keycloak**: An open-source identity and access management solution.
- **PostgreSQL**: A powerful, open-source relational database.
- **pgAdmin**: A web-based administration tool for PostgreSQL.

## Prerequisites

- Docker installed on your machine.
- Docker Compose installed.

## Getting Started

1. Clone the repository or download the project files to your local machine.

2. Navigate to the project directory:

   ```bash
   cd docker-compose-project
   ```

3. Build and start the services using Docker Compose:

   ```bash
   docker-compose up --build
   ```

4. Access the services:

   - **Movora.WebAPI**: [http://localhost:5000](http://localhost:5000)
   - **Keycloak**: [http://localhost:8080](http://localhost:8080)
   - **pgAdmin**: [http://localhost:5050](http://localhost:5050)

## Configuration

- The `docker-compose.yml` file defines the services and their configurations, including environment variables, ports, and volume mappings.
- Each service has its own Dockerfile located in the `services` directory.

## Stopping the Services

To stop the services, run:

```bash
docker-compose down
```

## Additional Information

Refer to the individual service directories for specific Dockerfile configurations and additional setup instructions if necessary.