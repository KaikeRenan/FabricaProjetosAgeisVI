FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY mvp.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish mvp.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "mvp.dll"]
