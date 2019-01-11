FROM microsoft/dotnet:sdk AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY ./ ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:aspnetcore-runtime
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "HealthcareSoftwareListing.dll"]

EXPOSE 5000

#Install-package Newtonsoft.Json

#      docker build -t HealthcareSoftwareListing .
#      docker run -it -p 5000:80 HealthcareSoftwareListing
