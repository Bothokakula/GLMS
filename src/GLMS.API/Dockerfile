FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/GLMS.Core/GLMS.Core.csproj src/GLMS.Core/
COPY src/GLMS.Infrastructure/GLMS.Infrastructure.csproj src/GLMS.Infrastructure/
COPY src/GLMS.API/GLMS.API.csproj src/GLMS.API/
RUN dotnet restore src/GLMS.API/GLMS.API.csproj

COPY src/ src/
RUN dotnet publish src/GLMS.API/GLMS.API.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/uploads
ENTRYPOINT ["dotnet", "GLMS.API.dll"]
