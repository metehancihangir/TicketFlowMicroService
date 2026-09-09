FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["TicketFlow.AuthService/TicketFlow.AuthService.csproj", "TicketFlow.AuthService/"]
COPY ["TicketFlow.Contracts/TicketFlow.Contracts.csproj", "TicketFlow.Contracts/"]
RUN dotnet restore "TicketFlow.AuthService/TicketFlow.AuthService.csproj"
COPY . .
WORKDIR "/src/TicketFlow.AuthService"
RUN dotnet publish "TicketFlow.AuthService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TicketFlow.AuthService.dll"]


