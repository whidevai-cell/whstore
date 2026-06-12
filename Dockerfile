FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# সলিউশন ফাইল এবং ভেতরের প্রজেক্ট ফাইল কপি
COPY ["whstore.sln", "./"]
COPY ["whstore/whstore.csproj", "whstore/"]
RUN dotnet restore "whstore/whstore.csproj"

COPY . .
WORKDIR "/src/whstore"
RUN dotnet build "whstore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "whstore.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "whstore.dll"]
