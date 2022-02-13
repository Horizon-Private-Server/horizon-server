FROM mcr.microsoft.com/dotnet/core/sdk:3.1

COPY . /src

RUN chmod a+x /src/entrypoint.sh

# -- DME
WORKDIR /src/Server.Dme
RUN dotnet publish -c Release -o out
WORKDIR /src/Server.Dme/out
# Generate config files
RUN timeout 1s dotnet Server.Dme.dll; exit 0;

# -- MAS/MLS/NAT
WORKDIR /src/Server.Medius
RUN dotnet publish -c Release -o out 
WORKDIR /src/Server.Medius/out
# Generate config files
RUN timeout 1s dotnet Server.Medius.dll; exit 0

# -- 
CMD "/src/entrypoint.sh"
