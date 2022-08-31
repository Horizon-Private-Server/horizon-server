#! /bin/sh

# Start DME
echo "Starting DME ..."
cd /dme/
dotnet Server.Dme.dll &

# Start MUIS
echo "Starting MUIS ..."
cd /muis/
dotnet Server.UniverseInformation.dll &

# Start MAS/MLS/NAT
echo "Starting Medius ..."
cd /medius/

echo "Sleeping before starting ..."
# This is so that the DB can start in time before server starts
sleep 3
echo "Starting ..."

dotnet Server.Medius.dll

