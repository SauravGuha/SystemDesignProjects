FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet publish --configuration Release --output Release

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS publish
WORKDIR /app
COPY --from=build /app/Release .

EXPOSE 8080
EXPOSE 443

ENTRYPOINT [ "dotnet", "UrlShortener.dll" ]
