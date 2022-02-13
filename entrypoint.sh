#! /bin/sh

# Start DME
echo "Starting DME ..."
cd /src/Server.Dme/out/
dotnet Server.Dme.dll &

# Start MAS/MLS/NAT
echo "Starting Medius ..."
cd /src/Server.Medius/out/
dotnet Server.Medius.dll
