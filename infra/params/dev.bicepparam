using '../main.bicep'

param namePrefix = 'subtrackdev'
param postgresAdminLogin = 'pgadmin'
param postgresAdminPassword = 'CHANGE_ME_STRONG_PASSWORD'
param apiImage = 'myregistry.azurecr.io/subscription-api:latest'
param workerImage = 'myregistry.azurecr.io/subscription-worker:latest'
