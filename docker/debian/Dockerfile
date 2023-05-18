# Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>
# Mewt is licensed under the terms of the AGPL-3.0-only license.
# See <https://github.com/mewt-server/mewt> or README.md file for details.

FROM mcr.microsoft.com/dotnet/sdk:7.0 as build
WORKDIR /src
COPY src/mewt/ ./
RUN dotnet restore
RUN dotnet publish --configuration Release --output /app --runtime linux-x64 --self-contained mewt.csproj

FROM mcr.microsoft.com/dotnet/runtime-deps:7.0-bullseye-slim as runtime
WORKDIR /mewt
COPY --from=build /app/mewt /usr/local/bin/mewt
COPY src/mewt/mewt.yml mewt.yml
RUN set -ex; \
    export DEBIAN_FRONTEND=noninteractive; \
    apt-get update; \
    apt-get upgrade -y --fix-missing --no-install-recommends curl git openssh-client rclone; \
    apt-get clean -y && apt-get clean -y && apt-get autoclean -y && rm -r /var/lib/apt/lists/*; \
    adduser --disabled-password --no-create-home --gecos mewt mewt; \
    chown mewt:mewt /mewt; \
    chmod 700 /mewt;
ENV ASPNETCORE_URLS=http://+:13523
ENV DOTNET_EnableDiagnostics=0
USER mewt
EXPOSE 13523
HEALTHCHECK --interval=15s --timeout=5s --start-period=1m --retries=3 CMD curl -f http://localhost:13523/healthcheck || exit 1
ENTRYPOINT ["/usr/local/bin/mewt"]
CMD [ ]