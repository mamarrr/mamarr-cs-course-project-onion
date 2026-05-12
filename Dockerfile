FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.Props ./
COPY App.BLL/App.BLL.csproj ./App.BLL/
COPY App.BLL.Contracts/App.BLL.Contracts.csproj ./App.BLL.Contracts/
COPY App.BLL.DTO/App.BLL.DTO.csproj ./App.BLL.DTO/
COPY App.DAL.Contracts/App.DAL.Contracts.csproj ./App.DAL.Contracts/
COPY App.DAL.DTO/App.DAL.DTO.csproj ./App.DAL.DTO/
COPY App.DAL.EF/App.DAL.EF.csproj ./App.DAL.EF/
COPY App.Domain/App.Domain.csproj ./App.Domain/
COPY App.DTO/App.DTO.csproj ./App.DTO/
COPY App.Resources/App.Resources.csproj ./App.Resources/
COPY Base.BLL/Base.BLL.csproj ./Base.BLL/
COPY Base.BLL.Contracts/Base.BLL.Contracts.csproj ./Base.BLL.Contracts/
COPY Base.Contracts/Base.Contracts.csproj ./Base.Contracts/
COPY Base.DAL.Contracts/Base.DAL.Contracts.csproj ./Base.DAL.Contracts/
COPY Base.DAL.EF/Base.DAL.EF.csproj ./Base.DAL.EF/
COPY Base.Domain/Base.Domain.csproj ./Base.Domain/
COPY Base.Helpers/Base.Helpers.csproj ./Base.Helpers/
COPY WebApp/WebApp.csproj ./WebApp/

RUN dotnet restore WebApp/WebApp.csproj

COPY . ./
RUN dotnet publish WebApp/WebApp.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "WebApp.dll"]
