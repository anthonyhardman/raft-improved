FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY ["Raft.Node/Raft.Node.csproj", "Raft.Node/"]
RUN dotnet restore "Raft.Node/Raft.Node.csproj"

COPY . .
WORKDIR /source/Raft.Node
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Raft.Node.dll"]