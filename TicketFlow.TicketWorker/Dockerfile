FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["TicketFlow.TicketWorker/TicketFlow.TicketWorker.csproj", "TicketFlow.TicketWorker/"]
COPY ["TicketFlow.Contracts/TicketFlow.Contracts.csproj", "TicketFlow.Contracts/"]
RUN dotnet restore "TicketFlow.TicketWorker/TicketFlow.TicketWorker.csproj"
COPY . .
WORKDIR "/src/TicketFlow.TicketWorker"
RUN dotnet publish "TicketFlow.TicketWorker.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TicketFlow.TicketWorker.dll"]
