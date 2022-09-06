# Horizon Private Server
This repository contains a Medius server emulator that works for a variety of games. Originally built to revive the Ratchet: Deadlocked PS2 online servers.

## Docker Compose
Docker Compose can be used to run the following containers:
1. Medius/DME/MUIS/NAT Servers run on the Dockerfile in this repo
2. [The Database Middleware](https://github.com/Horizon-Private-Server/horizon-server-database-middleware), which sits between the DB and the main Servers
3. SQL Server Database, which stores all the data

### Set up the configs, path, and environment variables
Docker Compose will expect a directory structure as follows (not all files/folders shown):
```
horizon/
├─ horizon-server/
│  ├─ docker/
│  │  ├─ dme.json
│  │  ├─ muis.json
│  │  ├─ medius.json
│  │  ├─ env.list
├─ horizon-database-middleware/
├─ horizon-database/
│  ├─ data/
│  ├─ log/
│  ├─ secrets/
```

1. Set up the configs in the `horizon-server/docker/` folder. You can change the ports, `PublicIpOverride` (if developing locally), or MUIS information if using MUIS. Don't forget to set the app ids in the `dme.config`!
2. Check the ports in `docker-compose.yml` to ensure you're exposing the same ports that are set in the config
3. Edit the env variables which will go to the database middleware / database containers

The data, logs, and secrets folders are to persist data so that data is not destroyed upon container destruction.

### Running
Run the `horizon-server/docker/run.sh` script from inside the docker folder. This will set the `env.list` environment variables and turn on the containers.

### Debugging the db
```
sqlcmd -S ${SERVER_IP},1433 -U sa -P "${YOUR_PASSWORD}"
```

### Misc
The default configs in the `horizon-server/docker/` folder were last generated 08/29/2022.
