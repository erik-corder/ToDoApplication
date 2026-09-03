FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ToDoApplication.Api.csproj .
RUN dotnet restore ToDoApplication.Api.csproj
COPY . .
RUN dotnet publish ToDoApplication.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ToDoApplication.Api.dll"]
