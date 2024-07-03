#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Tgs.Payment.UBPay/Tgs.Payment.UBPay.csproj", "Tgs.Payment.UBPay/"]
COPY ["UBPay.Core/UBPay.Core.csproj", "UBPay.Core/"]
COPY ["UBPay.Client/UBPay.Client.csproj", "UBPay.Client/"]
RUN dotnet restore "./Tgs.Payment.UBPay/Tgs.Payment.UBPay.csproj"
COPY . .
WORKDIR "/src/Tgs.Payment.UBPay"
RUN dotnet build "./Tgs.Payment.UBPay.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tgs.Payment.UBPay.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Payment.UBPay.dll"]