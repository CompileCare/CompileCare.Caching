FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["samples/CompileCare.Caching.SampleApi/CompileCare.Caching.SampleApi.csproj", "samples/CompileCare.Caching.SampleApi/"]
COPY ["src/CompileCare.Caching/CompileCare.Caching.csproj", "src/CompileCare.Caching/"]
RUN dotnet restore "samples/CompileCare.Caching.SampleApi/CompileCare.Caching.SampleApi.csproj"
COPY . .
WORKDIR "/src/samples/CompileCare.Caching.SampleApi"
RUN dotnet build "CompileCare.Caching.SampleApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "CompileCare.Caching.SampleApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CompileCare.Caching.SampleApi.dll"]