FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["FreeBudget.slnx", "."]
COPY ["src/", "src/"]

RUN dotnet restore "src/Services/Ledger/FreeBudget.Ledger.Api/FreeBudget.Ledger.Api.csproj"
RUN dotnet publish "src/Services/Ledger/FreeBudget.Ledger.Api/FreeBudget.Ledger.Api.csproj"     -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FreeBudget.Ledger.Api.dll"]
