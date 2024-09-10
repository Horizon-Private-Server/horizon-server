#! /bin/sh
echo "Starting ..."

cp /db.config.json /medius/
cp /db.config.json /dme/

cd /
python restart_dme.py > restart_dme.log 2>&1 &

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
