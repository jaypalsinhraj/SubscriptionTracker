# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/backend/SubscriptionLeakDetector.sln ./
COPY src/backend/SubscriptionLeakDetector.Domain/SubscriptionLeakDetector.Domain.csproj SubscriptionLeakDetector.Domain/
COPY src/backend/SubscriptionLeakDetector.Application/SubscriptionLeakDetector.Application.csproj SubscriptionLeakDetector.Application/
COPY src/backend/SubscriptionLeakDetector.Infrastructure/SubscriptionLeakDetector.Infrastructure.csproj SubscriptionLeakDetector.Infrastructure/
COPY src/backend/SubscriptionLeakDetector.Api/SubscriptionLeakDetector.Api.csproj SubscriptionLeakDetector.Api/

RUN dotnet restore SubscriptionLeakDetector.Api/SubscriptionLeakDetector.Api.csproj

COPY src/backend/ ./

WORKDIR /src/SubscriptionLeakDetector.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SubscriptionLeakDetector.Api.dll"]
