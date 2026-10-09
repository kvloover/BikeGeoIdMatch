FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BikeFit.Web/BikeFit.Web.csproj BikeFit.Web/
RUN dotnet restore BikeFit.Web/BikeFit.Web.csproj
COPY BikeFit.Web/ BikeFit.Web/
RUN dotnet publish BikeFit.Web/BikeFit.Web.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
# the app writes nothing to disk, so the container can run read-only and as a non-root user
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "BikeFit.Web.dll"]
