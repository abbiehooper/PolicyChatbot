# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PolicyChatbot.sln .
COPY Server/PolicyChatbot.Server.csproj Server/
COPY Client/PolicyChatbot.Client.csproj Client/
COPY Shared/PolicyChatbot.Shared.csproj Shared/

RUN dotnet restore

COPY . .

WORKDIR /src/Server
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/PolicyDocuments
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "PolicyChatbot.Server.dll"]