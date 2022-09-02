#! /bin/sh
echo "Sleeping before starting ..."
# This is so that the DB can start in time before server starts
sleep 30
echo "Starting ..."

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
dotnet Server.Medius.dll
