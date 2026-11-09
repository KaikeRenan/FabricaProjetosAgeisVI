FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY mvp.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish mvp.csproj -c Release -o /out --no-restore

# One-shot service used by docker compose to apply EF Core migrations.
FROM build AS migrate
RUN dotnet tool install --global dotnet-ef --version 9.0.2
ENV PATH="$PATH:/root/.dotnet/tools"
ENTRYPOINT ["dotnet", "ef", "database", "update"]

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "mvp.dll"]
