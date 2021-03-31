# dotnet serverless auth

This demo uses the State of Utah Open Id Connect and an Authorization Code flow with PKCE for authentication.

Once authenticated, auth is swapped to a dotnet managed cookie and the authentication tickets are stored in a distributed redis cache along with the data protection key to decrypt the auth cookie. This allows any dotnet process with access to the distributed cache to authenticate clients which is great for serverless or load balanced scenarios as no auth information is stored in memory.

This demo is built to run locally (with a redis installation or container), completely in docker, or in GCP with Cloud Run and a redis Memorystore.

## Getting Started

### Authentication Setup

1. Request an apadmin.utah.gov app
1. Create user fields in the schema tab

   - for this app there is a `UserRole` `OPTION` and `administrator`, etc `OPTIONS`

1. Create a client for that app
1. Add `openid` and `app:{yourApp}` as scopes
1. Toggle `Implied Consent` on
1. Select `Authorization Code` Grant type
1. Add Redirection urls for your localhost or cloud run app

   - they will be in the form of `https://localhost:5001/signin-oidc`

1. Click open on the `app:{yourApp}` and grant read access to the user fields

### OpenId Connect Setup

1. set the environment variable for the apadmin client id and secret

   - in development you can use dotnet user secrets

      ```sh
      dotnet user-secrets set "Authentication:UtahId:ClientId" "your id"
      dotnet user-secrets set "Authentication:UtahId:ClientSecret" "your secret"
      ```

   - docker-compose.override.yaml

     ```yaml
      api:
        environment:
          - Authentication__UtahId__ClientId=
          - Authentication__UtahId__ClientSecret=
        ```

### Memorystore (Redis) Setup

1. set the environment variable the redis memory store connection

   - in development you can use dotnet user secrets

      ```sh
      dotnet user-secrets set "Redis:Configuration" "localhost:6379"
      ```

   - docker-compose.override.yaml

     ```yaml
      api:
        environment:
          - Redis__Configuration=redis
     ```

1. Open the ports for redis

   - docker-compose.override.yaml

     ```yaml
     redis:
       ports:
         - "6379:6379"
     ```

### IP Geolocation Setup

1. Create an account with [maxmind.com](https://www.maxmind.com/)
1. Generate a license
1. Add the dotnet user secrets or set them as environment variables

   - dotnet user secrets

      ```sh
        dotnet user-secrets set "MaxMind:AccountId" ####
        dotnet user-secrets set "MaxMind:LicenseKey" "your license"
        dotnet user-secrets set "MaxMind:Timeout" 3600
        dotnet user-secrets set "MaxMind:Host" "geolite.info"
      ```

   - docker-compose.override.yaml

     ```yaml
      api:
        environment:
          - MaxMind__AccountId=
          - MaxMind__LicenseKey=
          - MaxMind__Timeout=3600
          - MaxMind__Host=geolite.info
        ```

### Docker Setup

For docker to work with this flow the dotnet developer certificate needs to be accessible to kestrel.

1. Create a docker volume that points to your pfx store for the dotnet sdk

   - docker-compose.override.yaml

      ```yaml
      api:
        volumes:
          - ${HOME}/.aspnet/https:/https
      ```

1. Generate a dev cert to use

   ```sh
   dotnet dev-certs https ${HOME}/.aspnet/https/auth-ticket.pfx -p some-password
   ```

1. Add the environment variables to use the certificate

   - docker-compose.override.yaml

      ```yaml
      api:
        environment:
          - Kestrel__Certificates__Default__Password=some-password
          - Kestrel__Certificates__Default__Path=/https/auth-ticket.pf
      ```

1. Tell docker what ports to allow traffic on

   - docker-compose.override.yaml

      ```yaml
      api:
        ports:
          - 5001:5001
        environment:
          - ASPNETCORE_URLS=https://+:5001
      ```

## Building

- Using VS Code
  - Run the Build task

- Using the scripts

   ```sh
   ./scripts/build.sh
   ```

- Using docker compose

   ```sh
   docker-compose build
   ```

## Running

- Using VS Code
  - F5

- Using docker compose

   ```sh
   docker-compose up
   ```

## Publishing

The publish script pushes the docker image to GCR

- Using the scripts

   ```sh
   ./scripts/publish.sh
   ```

## Infrastructure

1. Initialize terraform

   ```sh
   cd _infrastructure
   terraform init
   ```

1. Stand up infrastructure

   ```sh
   terraform apply
   ```

## Cloud Run

1. Choose the image from GCR
1. Capacity
   - Memory `128 MiB`
   - CPU `1`
   - Request Timeout `10`
   - Maximum requests per container `250`
1. Autoscaling
   - Minimum `0`
   - Maximum `4`
1. Connections
   - VPC Connector
     - Choose the serverless VPC connector

1. Use the same environment variables as you would for docker but use the real memory store ip and port.
