# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/backend/SubscriptionLeakDetector.sln ./
COPY src/backend/SubscriptionLeakDetector.Domain/SubscriptionLeakDetector.Domain.csproj SubscriptionLeakDetector.Domain/
COPY src/backend/SubscriptionLeakDetector.Application/SubscriptionLeakDetector.Application.csproj SubscriptionLeakDetector.Application/
COPY src/backend/SubscriptionLeakDetector.Infrastructure/SubscriptionLeakDetector.Infrastructure.csproj SubscriptionLeakDetector.Infrastructure/
COPY src/backend/SubscriptionLeakDetector.Worker/SubscriptionLeakDetector.Worker.csproj SubscriptionLeakDetector.Worker/

RUN dotnet restore SubscriptionLeakDetector.Worker/SubscriptionLeakDetector.Worker.csproj

COPY src/backend/ ./

WORKDIR /src/SubscriptionLeakDetector.Worker
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SubscriptionLeakDetector.Worker.dll"]
