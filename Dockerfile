FROM mcr.microsoft.com/dotnet/sdk:latest AS build
WORKDIR /app

# copy csproj and restore as distinct layers
# copy ALL the projects
COPY App.BLL/*.csproj ./App.BLL/.
COPY App.Contracts/*.csproj ./App.Contracts/.
COPY App.DAL.EF/*.csproj ./App.DAL.EF/.
COPY App.Domain/*.csproj ./App.Domain/.
COPY App.DTO/*.csproj ./App.DTO/.
COPY App.Resources/*.csproj ./App.Resources/.
COPY Base.Contracts/*.csproj ./Base.Contracts/.
COPY Base.Domain/*.csproj ./Base.Domain/.
COPY Base.Helpers/*.csproj ./Base.Helpers/.
COPY WebApp/*.csproj ./WebApp/.
RUN dotnet restore WebApp/WebApp.csproj

# copy everything else and build app
# copy all the projects
COPY App.BLL/. ./App.BLL/
COPY App.Contracts/. ./App.Contracts/
COPY App.DAL.EF/. ./App.DAL.EF/
COPY App.Domain/. ./App.Domain/
COPY App.DTO/. ./App.DTO/
COPY App.Resources/. ./App.Resources/
COPY Base.Contracts/. ./Base.Contracts/
COPY Base.Domain/. ./Base.Domain/
COPY Base.Helpers/. ./Base.Helpers/
COPY WebApp/. ./WebApp/
WORKDIR /app/WebApp
RUN dotnet publish -c Release -o out


FROM mcr.microsoft.com/dotnet/aspnet:latest AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/WebApp/out ./
ENV ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=apartmentcrmsaas;Username=postgres;Password=postgres
ENTRYPOINT ["dotnet", "WebApp.dll"]
