#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["samples/Rene.Utils.Db.Sample.AutoMapper/Rene.Utils.Db.Sample.AutoMapper.csproj", "samples/Rene.Utils.Db.Sample.AutoMapper/"]
RUN dotnet restore "./samples/Rene.Utils.Db.Sample.AutoMapper/Rene.Utils.Db.Sample.AutoMapper.csproj"
COPY . .
WORKDIR "/src/samples/Rene.Utils.Db.Sample.AutoMapper"
RUN dotnet build "./Rene.Utils.Db.Sample.AutoMapper.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Rene.Utils.Db.Sample.AutoMapper.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Rene.Utils.Db.Sample.AutoMapper.dll"]