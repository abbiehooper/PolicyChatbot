# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY PolicyChatbot.sln .
COPY Server/PolicyChatbot.Server.csproj Server/
COPY Client/PolicyChatbot.Client.csproj Client/
COPY Shared/PolicyChatbot.Shared.csproj Shared/

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY . .

# Build and publish
WORKDIR /src/Server
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Copy policy documents
COPY Server/PolicyDocuments /app/PolicyDocuments

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "PolicyChatbot.Server.dll"]