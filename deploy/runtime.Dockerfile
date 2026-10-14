FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim
WORKDIR /app
COPY publish/ ./
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "Zuni.dll"]
