# Build context: ./backend (see compose.yaml)

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes
COPY PaymentAssessment.sln ./
COPY src/PaymentAssessment.Domain/PaymentAssessment.Domain.csproj src/PaymentAssessment.Domain/
COPY src/PaymentAssessment.Application/PaymentAssessment.Application.csproj src/PaymentAssessment.Application/
COPY src/PaymentAssessment.Infrastructure/PaymentAssessment.Infrastructure.csproj src/PaymentAssessment.Infrastructure/
COPY src/PaymentAssessment.Api/PaymentAssessment.Api.csproj src/PaymentAssessment.Api/
RUN dotnet restore PaymentAssessment.sln

COPY . .
RUN dotnet publish src/PaymentAssessment.Api/PaymentAssessment.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PaymentAssessment.Api.dll"]
