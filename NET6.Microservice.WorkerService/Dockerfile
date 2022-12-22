#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
RUN mkdir -p /app/logs
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY ["NET6.Microservice.Core/NET6.Microservice.Core.csproj", "NET6.Microservice.Core/NET6.Microservice.Core.csproj"]
COPY ["NET6.Microservice.Messages/NET6.Microservice.Messages.csproj", "NET6.Microservice.Messages/NET6.Microservice.Messages.csproj"]
COPY ["NET6.Microservice.WorkerService/NET6.Microservice.WorkerService.csproj", "NET6.Microservice.WorkerService/"]

RUN dotnet restore "NET6.Microservice.WorkerService/NET6.Microservice.WorkerService.csproj"
COPY . .
WORKDIR "/src/NET6.Microservice.WorkerService"
RUN dotnet build "NET6.Microservice.WorkerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NET6.Microservice.WorkerService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NET6.Microservice.WorkerService.dll"]