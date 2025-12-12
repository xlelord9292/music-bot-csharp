# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore --runtime linux-musl-x64

# Copy source code and build
COPY . .
RUN dotnet publish -c Release -o /app --runtime linux-musl-x64 --self-contained false --no-restore

# Runtime stage - using alpine for smallest image size
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS runtime
WORKDIR /app

# Install dependencies for better performance
RUN apk add --no-cache icu-libs

# Set environment variables for performance
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_gcServer=1 \
    DOTNET_GCHeapHardLimit=0x10000000

# Copy built application
COPY --from=build /app .

# Run as non-root user for security
RUN adduser -D -h /app musico && chown -R musico:musico /app
USER musico

# Start the bot
ENTRYPOINT ["dotnet", "Musico.dll"]
