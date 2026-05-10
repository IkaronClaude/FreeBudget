# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["FreeBudget.slnx", "."]
COPY ["global.json", "."]
COPY ["src/", "src/"]

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore "src/Services/Identity/FreeBudget.Identity.Api/FreeBudget.Identity.Api.csproj"

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish "src/Services/Identity/FreeBudget.Identity.Api/FreeBudget.Identity.Api.csproj" \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FreeBudget.Identity.Api.dll"]
