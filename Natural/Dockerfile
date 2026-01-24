# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Natural/Natural.csproj", "Natural/"]
RUN dotnet restore "Natural/Natural.csproj"

COPY . .
WORKDIR "/src/Natural"
RUN dotnet publish "Natural.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Natural.dll"]
