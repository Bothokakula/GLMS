FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/GLMS.Core/GLMS.Core.csproj src/GLMS.Core/
COPY src/GLMS.Web/GLMS.Web.csproj src/GLMS.Web/
RUN dotnet restore src/GLMS.Web/GLMS.Web.csproj

COPY src/ src/
RUN dotnet publish src/GLMS.Web/GLMS.Web.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GLMS.Web.dll"]
