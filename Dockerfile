FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG VERSION=0.0.0
ARG ASSEMBLY_VERSION=0.0.0

COPY PedidosApi.csproj .

RUN dotnet restore PedidosApi.csproj

COPY . .

RUN dotnet publish PedidosApi.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    -p:Version=${ASSEMBLY_VERSION} \
    -p:AssemblyVersion=${ASSEMBLY_VERSION} \
    -p:FileVersion=${ASSEMBLY_VERSION} \
    -p:InformationalVersion=${VERSION} \
    -p:IncludeSourceRevisionInformationalVersion=false


# Estágio de runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

USER 1000

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "PedidosApi.dll"]