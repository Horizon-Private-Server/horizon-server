#! /bin/sh
echo "Sleeping before starting ..."
# This is so that the DB can start in time before server starts
sleep 30
echo "Starting ..."

echo "Setting admin username/pw ..."
curl --insecure -X POST "${MIDDLEWARE_SERVER_IP}/Account/createAccount" -H  "accept: */*" -H  "Content-Type: application/json-patch+json" -d "{\"AccountName\":\"testAccountName2\",\"AccountPassword\":\"testAccountPW\",\"MachineId\":\"1\",\"MediusStats\":\"1\",\"AppId\":0,\"PasswordPreHashed\":false}"

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

