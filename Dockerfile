# Build stage =========================================================================
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder

COPY . /src

#====== Build DME
WORKDIR /src/Server.Dme
RUN dotnet publish -c Release -o out

#===== Build MAS/MLS
WORKDIR /src/Server.Medius
RUN dotnet publish -c Release -o out

#===== Build NAT
WORKDIR /src/Server.NAT
RUN dotnet publish -c Release -o out

#===== Build MUIS
WORKDIR /src/Server.UniverseInformation
RUN dotnet publish -c Release -o out

# Copy configs
RUN cp /src/docker/dme.json /src/Server.Dme/out/dme.json
RUN cp /src/docker/medius.json /src/Server.Medius/out/medius.json
RUN cp /src/docker/muis.json /src/Server.UniverseInformation/out/muis.json

# Copy patch and plugins into right folders
RUN mkdir -p /src/Server.Medius/out/plugins/
RUN mkdir -p /src/Server.Dme/out/plugins/

RUN cp -r /src/docker/medius_plugins/* /src/Server.Medius/out/plugins/
RUN cp -r /src/docker/dme_plugins/* /src/Server.Dme/out/plugins/

# Run stage =========================================================================

FROM mcr.microsoft.com/dotnet/core/sdk:3.1
RUN mkdir /logs
COPY ./docker /docker
RUN chmod a+x /docker/entrypoint.sh

COPY --from=builder /src/docker/restart_dme.py /
COPY --from=builder /src/Server.Dme/out /dme
COPY --from=builder /src/Server.Medius/out /medius
COPY --from=builder /src/Server.NAT/out /nat
COPY --from=builder /src/Server.UniverseInformation/out /muis
COPY --from=builder /src/docker /configs

CMD "/docker/entrypoint.sh"
