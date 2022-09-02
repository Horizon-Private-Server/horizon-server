# Build stage =========================================================================
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder

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

# Copy configs
RUN cp /src/docker/dme.json /src/Server.Dme/out/dme.json
RUN cp /src/docker/medius.json /src/Server.Medius/out/medius.json
RUN cp /src/docker/muis.json /src/Server.UniverseInformation/out/muis.json

# Run stage =========================================================================

FROM mcr.microsoft.com/dotnet/core/sdk:3.1
COPY ./docker /docker
RUN chmod a+x /docker/entrypoint.sh

COPY --from=builder /src/Server.Dme/out /dme
COPY --from=builder /src/Server.Medius/out /medius
COPY --from=builder /src/Server.UniverseInformation/out /muis
COPY --from=builder /src/docker /configs

CMD "/docker/entrypoint.sh"
