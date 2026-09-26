FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY backend/src/Carmasters.sln .
COPY backend/src/. .

RUN dotnet restore Carmasters.sln
RUN dotnet publish DbUp/DbUp.csproj -c Release -o /dbup

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /dbup .
ENTRYPOINT ["dotnet","DbUp.dll"]
