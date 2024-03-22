FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY ["Raft.Shop/Raft.Shop.csproj", "Raft.Shop/"]
RUN dotnet restore "Raft.Shop/Raft.Shop.csproj"

COPY . .
WORKDIR /source/Raft.Shop
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Raft.Shop.dll"]