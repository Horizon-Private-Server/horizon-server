# Build stage =========================================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS builder

COPY . /src

#====== Build DME
WORKDIR /src/Server.Dme
RUN dotnet publish -c Release -o out

#===== Build MAS/MLS/NAT
WORKDIR /src/Server.Medius
RUN dotnet publish -c Release -o out

#===== Build MUIS
WORKDIR /src/Server.UniverseInformation
RUN dotnet publish -c Release -o out

# Copy patch and plugins into right folders
# RUN mkdir -p /src/Server.Medius/out/plugins/
# RUN mkdir -p /src/Server.Dme/out/plugins/

# RUN cp -r /src/docker/medius_plugins/* /src/Server.Medius/out/plugins/
# RUN cp -r /src/docker/dme_plugins/* /src/Server.Dme/out/plugins/

# Run stage =========================================================================

FROM mcr.microsoft.com/dotnet/sdk:9.0
RUN mkdir /logs
COPY ./docker /docker
RUN chmod a+x /docker/entrypoint.sh

COPY --from=builder /src/docker/restart_dme.py /
COPY --from=builder /src/Server.Dme/out /dme
COPY --from=builder /src/Server.Medius/out /medius
COPY --from=builder /src/Server.UniverseInformation/out /muis
COPY --from=builder /src/docker /configs

CMD "/docker/entrypoint.sh"
