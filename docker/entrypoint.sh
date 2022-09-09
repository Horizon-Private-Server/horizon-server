#! /bin/sh
echo "Sleeping before starting ..."
# This is so that the DB can start in time before server starts
sleep 30
echo "Starting ..."

# Configure db.config.json
sed -i "s|\"SimulatedMode\": true|\"SimulatedMode\": false|g" /configs/db.config.json
sed -i "s|\"DatabaseUrl\": \"http://localhost:80\"|\"DatabaseUrl\": \"${MIDDLEWARE_SERVER_IP}\"|g" /configs/db.config.json
sed -i "s|\"DatabaseUsername\": null|\"DatabaseUsername\": \"${MIDDLEWARE_USER}\"|g" /configs/db.config.json
sed -i "s|\"DatabasePassword\": null|\"DatabasePassword\": \"${MIDDLEWARE_PASSWORD}\"|g" /configs/db.config.json

sed -i "s|\"ApplicationIds\": \[\],|\"ApplicationIds\": \[${APP_ID}\],|g" /configs/dme.json

cp /configs/db.config.json /dme/
cp /configs/db.config.json /medius/

cp /configs/dme.json /dme/

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
