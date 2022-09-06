# Horizon Private Server
This repository contains a Medius server emulator that works for a variety of games. Originally built to revive the Ratchet: Deadlocked PS2 online servers.

## Docker Compose
Docker Compose can be used to run the following containers:
1. Medius/DME/MUIS/NAT Servers run on the Dockerfile in this repo
2. [The Database Middleware](https://github.com/Horizon-Private-Server/horizon-server-database-middleware), which sits between the DB and the main Servers
3. SQL Server Database, which stores all the data

### Set up the configs, directory structure, and environment variables
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

| Environment Variable   | Description                                                                                                         |
|------------------------|---------------------------------------------------------------------------------------------------------------------|
| DB_USER                | The admin user to login to the database as (will create if it doesn't exist)                                        |
| MSSQL_SA_PASSWORD      | The user's password to login to the database                                                                        |
| DB_NAME                | The name of the database to connect to                                                                              |
| DB_SERVER              | The local or external DB port, e.g. 192.168.1.2,1433                                                                |
| ASPNETCORE_ENVIRONMENT | The build ASP.NET core environment                                                                                  |
| MIDDLEWARE_SERVER      | The IP to bind to, generally looks like http://0.0.0.0:10000                                                        |
| MIDDLEWARE_SERVER_IP   | The external or local IP that will be hosting this. Can also be a docker internal IP. E.g. http://192.168.1.2:10000 |
| MIDDLEWARE_USER        | The name of the 'admin' middleware user. This will get stored as a medius account                                   |
| MIDDLEWARE_PASSWORD    | The password for the middleware admin user                                                                          |
| APP_ID                 | The APP id to host. Can be a comma separated list                                                                   |

### Running
Run the `horizon-server/docker/run.sh` script from inside the docker folder. This will set the `env.list` environment variables and turn on the containers.

### Debugging the db
```
sqlcmd -S ${SERVER_IP},1433 -U sa -P "${YOUR_PASSWORD}"
```

### Misc
The default configs in the `horizon-server/docker/` folder were last generated 08/29/2022.
