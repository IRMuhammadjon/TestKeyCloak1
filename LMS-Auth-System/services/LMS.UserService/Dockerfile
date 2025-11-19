FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/LMS.UserService/LMS.UserService.csproj", "services/LMS.UserService/"]
COPY ["shared/LMS.Shared.TokenValidation/LMS.Shared.TokenValidation.csproj", "shared/LMS.Shared.TokenValidation/"]
RUN dotnet restore "services/LMS.UserService/LMS.UserService.csproj"
COPY . .
WORKDIR "/src/services/LMS.UserService"
RUN dotnet build "LMS.UserService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LMS.UserService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LMS.UserService.dll"]
