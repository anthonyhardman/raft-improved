FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY ["Raft.GateWay/Raft.GateWay.csproj", "Raft.GateWay/"]
RUN dotnet restore "Raft.GateWay/Raft.GateWay.csproj"

COPY . .
WORKDIR /source/Raft.GateWay
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Raft.GateWay.dll"]