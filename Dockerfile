FROM mcr.microsoft.com/dotnet/core/sdk:2.2-alpine AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln .
COPY Application/*.csproj ./Application/
COPY ApplicationUnitTests/*.csproj ./ApplicationUnitTests/
COPY lib/utility/Application.Utility/*.csproj lib/utility/Application.Utility/
RUN dotnet restore

# copy everything else and build app
COPY /. ./Application/
WORKDIR /app/Application
RUN dotnet publish -c Release -o out


FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-alpine AS runtime
WORKDIR /app
COPY --from=build /app/Application/Application/out ./
ENTRYPOINT ["dotnet", "Application.dll"]