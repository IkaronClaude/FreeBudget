FROM busybox AS csproj-source
WORKDIR /src
COPY src/ ./src/
RUN find /src -type f ! -name '*.csproj' -delete && find /src -type d -empty -delete

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["Directory.Build.props", "Directory.Packages.props", "FreeBudget.slnx", "global.json", "./"]
COPY --from=csproj-source /src/src ./src

RUN dotnet restore "src/Services/Transactions/FreeBudget.Transactions.Api/FreeBudget.Transactions.Api.csproj"

COPY src/ src/

RUN dotnet publish "src/Services/Transactions/FreeBudget.Transactions.Api/FreeBudget.Transactions.Api.csproj" \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FreeBudget.Transactions.Api.dll"]
