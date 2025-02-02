FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS prepare-restore-files
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install --global --no-cache dotnet-subset --version 0.3.2
WORKDIR /app/src/
COPY . .
RUN dotnet subset restore "./HttpApi/HttpApi.csproj" --root-directory /app/src --output restore_subset/

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app/src
COPY --from=prepare-restore-files app/src/restore_subset .
RUN dotnet restore "./HttpApi/HttpApi.csproj"
COPY . .
RUN dotnet build "./HttpApi/HttpApi.csproj" -c Release -o /app/build
# For healthchecks
RUN apt-get update && apt-get install -y curl

FROM build AS publish
RUN dotnet publish "./HttpApi/HttpApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HttpApi.dll"]