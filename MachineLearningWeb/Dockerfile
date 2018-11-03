FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 50920
EXPOSE 44310

RUN apt-get update
RUN apt-get install -y libgdiplus

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["MachineLearningWeb/MachineLearningWeb.csproj", "MachineLearningWeb/"]
RUN dotnet restore "MachineLearningWeb/MachineLearningWeb.csproj"
COPY . .
WORKDIR "/src/MachineLearningWeb"
RUN dotnet build "MachineLearningWeb.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "MachineLearningWeb.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MachineLearningWeb.dll"]