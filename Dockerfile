FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder

# Args
ARG USE_PUBLIC_IP=false
ARG PUBLIC_IP_OVERRIDE=192.168.1.2
ARG MASPort=10075
ARG MLSPort=10078
ARG MPSPort=10077
ARG NATIp=null
ARG NATPort=10070

COPY . /src

# Delete any configs if they exist
RUN find . -name "config.json" -exec rm {} \;

#====== Build DME
WORKDIR /src/Server.Dme
RUN dotnet publish -c Release -o out
WORKDIR /src/Server.Dme/out
# Generate default config files
RUN timeout 1s dotnet Server.Dme.dll; exit 0;

#===== Build MAS/MLS/NAT
WORKDIR /src/Server.Medius
RUN dotnet publish -c Release -o out 
WORKDIR /src/Server.Medius/out
# Generate default config files
RUN timeout 1s dotnet Server.Medius.dll; exit 0

#===== Build MUIS
WORKDIR /src/Server.UniverseInformation
RUN dotnet publish -c Release -o out 
WORKDIR /src/Server.UniverseInformation/out
# Generate default config files
RUN timeout 1s dotnet Server.UniverseInformation.dll; exit 0


# Set config based on args
RUN find /src -name "config.json" -exec sed -i 's|"UsePublicIp": false|"UsePublicIp": '"${USE_PUBLIC_IP}"'|g' {} \;
RUN find /src -name "config.json" -exec sed -i 's|"PublicIpOverride": ""|"PublicIpOverride": "'"${PUBLIC_IP_OVERRIDE}"'"|g' {} \;

RUN find /src -name "config.json" -exec sed -i 's|"MASPort": 10075|"MASPort": '"${MASPort}"'|g' {} \;
RUN find /src -name "config.json" -exec sed -i 's|"MLSPort": 10078|"MLSPort": '"${MLSPort}"'|g' {} \;
RUN find /src -name "config.json" -exec sed -i 's|"MPSPort": 10077|"MPSPort": '"${MPSPort}"'|g' {} \;
RUN find /src -name "config.json" -exec sed -i 's|"NATIp": null|"NATIp": '"${NATIp}"'|g' {} \;
RUN find /src -name "config.json" -exec sed -i 's|"NATPort": 10070|"NATPort": '"${NATPort}"'|g' {} \;




FROM mcr.microsoft.com/dotnet/core/sdk:3.1
COPY ./docker /docker
RUN chmod a+x /docker/entrypoint.sh

COPY --from=builder /src/Server.Dme/out /dme
COPY --from=builder /src/Server.Medius/out /medius
COPY --from=builder /src/Server.UniverseInformation/out /muis

CMD "/docker/entrypoint.sh"
