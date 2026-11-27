FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["samples/HeapCare.Caching.SampleApi/HeapCare.Caching.SampleApi.csproj", "samples/HeapCare.Caching.SampleApi/"]
COPY ["src/HeapCare.Caching/HeapCare.Caching.csproj", "src/HeapCare.Caching/"]
RUN dotnet restore "samples/HeapCare.Caching.SampleApi/HeapCare.Caching.SampleApi.csproj"
COPY . .
WORKDIR "/src/samples/HeapCare.Caching.SampleApi"
RUN dotnet build "HeapCare.Caching.SampleApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HeapCare.Caching.SampleApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HeapCare.Caching.SampleApi.dll"]