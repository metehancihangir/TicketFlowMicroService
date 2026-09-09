FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["TicketFlow.AnalyticsService/TicketFlow.AnalyticsService.csproj", "TicketFlow.AnalyticsService/"]
COPY ["TicketFlow.Contracts/TicketFlow.Contracts.csproj", "TicketFlow.Contracts/"]
RUN dotnet restore "TicketFlow.AnalyticsService/TicketFlow.AnalyticsService.csproj"
COPY . .
WORKDIR "/src/TicketFlow.AnalyticsService"
RUN dotnet publish "TicketFlow.AnalyticsService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TicketFlow.AnalyticsService.dll"]


