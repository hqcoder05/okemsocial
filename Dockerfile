FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Configure permissions for the built-in 'app' user
RUN mkdir -p /app/wwwroot/uploads/images /app/wwwroot/uploads/videos \
    && chown -R app:app /app/wwwroot/uploads

USER app
ENV PORT=5070
EXPOSE 5070

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Okem-Social.csproj", "."]
RUN dotnet restore "./Okem-Social.csproj"

COPY . .
WORKDIR "/src"
RUN dotnet build "./Okem-Social.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Okem-Social.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish --chown=app:app /app/publish .

ENTRYPOINT ["dotnet", "Okem-Social.dll"]
