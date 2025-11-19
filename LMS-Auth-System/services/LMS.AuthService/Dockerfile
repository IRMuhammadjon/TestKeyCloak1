FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/LMS.AuthService/LMS.AuthService.csproj", "services/LMS.AuthService/"]
COPY ["shared/LMS.Shared.TokenValidation/LMS.Shared.TokenValidation.csproj", "shared/LMS.Shared.TokenValidation/"]
RUN dotnet restore "services/LMS.AuthService/LMS.AuthService.csproj"
COPY . .
WORKDIR "/src/services/LMS.AuthService"
RUN dotnet build "LMS.AuthService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LMS.AuthService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LMS.AuthService.dll"]
