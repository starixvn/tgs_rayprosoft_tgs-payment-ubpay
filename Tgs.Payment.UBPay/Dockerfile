#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Payment.UBPay/Tgs.Payment.UBPay.csproj", "Tgs.Payment.UBPay/"]
COPY ["UBPay.Core/UBPay.Core.csproj", "UBPay.Core/"]
COPY ["UBPay.Client/UBPay.Client.csproj", "UBPay.Client/"]
RUN dotnet restore "./Tgs.Payment.UBPay/./Tgs.Payment.UBPay.csproj"
COPY . .
WORKDIR "/src/Tgs.Payment.UBPay"
RUN dotnet build "./Tgs.Payment.UBPay.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration=Release
RUN dotnet publish "./Tgs.Payment.UBPay.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Payment.UBPay.dll"]