FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY api/Colegio.Api.csproj api/
RUN dotnet restore api/Colegio.Api.csproj
COPY api/ api/
RUN dotnet publish api/Colegio.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Colegio.Api.dll"]
