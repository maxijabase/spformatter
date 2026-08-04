# syntax=docker/dockerfile:1

ARG GRAMMAR_REPO=https://github.com/maxijabase/tree-sitter-sourcepawn.git
ARG GRAMMAR_REF=main

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY SpFormatter.slnx ./
COPY src/SpFormatter ./src/SpFormatter
COPY src/SpModernizer ./src/SpModernizer
COPY src/SpFormatter.Playground ./src/SpFormatter.Playground
RUN dotnet restore src/SpFormatter.Playground/SpFormatter.Playground.csproj
RUN dotnet publish src/SpFormatter.Playground/SpFormatter.Playground.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS grammar
ARG GRAMMAR_REPO
ARG GRAMMAR_REF
RUN apt-get update \
    && apt-get install -y --no-install-recommends gcc libc6-dev git \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /grammar
RUN git clone --depth 1 --branch "${GRAMMAR_REF}" "${GRAMMAR_REPO}" .
RUN gcc -shared -fPIC -O2 -I src \
    -o tree-sitter-sourcepawn.so \
    src/parser.c src/scanner.c

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=grammar /grammar/tree-sitter-sourcepawn.so .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SpFormatter.Playground.dll"]
