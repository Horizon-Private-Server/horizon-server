# Horizon Private Server
This repository contains a Medius server emulator that works for a variety of games. Originally built to revive the Ratchet: Deadlocked PS2 online servers.

## Docker
The following scripts are meant for Linux or Windows WSL. You will need the following repositories to run the server:
1. This repo
2. The [horizon-server-database repo](https://github.com/Horizon-Private-Server/horizon-server-database)
3. The [horizon-server-database-middleware repo](https://github.com/Horizon-Private-Server/horizon-server-database-middleware)

The first thing to do is clone these repositories. Place the repos into a parent "horizon" directory. Your directory structure should look like the following:
```
├── horizon/
│   └── horizon-server/
│   └── horizon-server-database/
│   └── horizon-server-database-middleware/
```

Now, add the following environment variables. I like to edit `~/.bashrc` so they are always loaded on a new terminal.
| Environment Variable   | Description                                                                                                         |
|------------------------|---------------------------------------------------------------------------------------------------------------------|
| HORIZON_DB_USER                | The admin user to login to the database as (will create if it doesn't exist)                                        |
| HORIZON_MSSQL_SA_PASSWORD      | The user's password to login to the database                                                                        |
| HORIZON_DB_NAME                | The name of the database to connect to                                                                              |
| HORIZON_DB_SERVER              | The local or external DB port, e.g. 192.168.1.2,1433                                                                |
| HORIZON_ASPNETCORE_ENVIRONMENT | The build ASP.NET core environment                                                                                  |
| HORIZON_MIDDLEWARE_SERVER      | The IP to bind to, generally looks like http://0.0.0.0:10000                                                        |
| HORIZON_MIDDLEWARE_SERVER_IP   | The external or local IP that will be hosting this. Can also be a docker internal IP. E.g. http://192.168.1.2:10000 |
| HORIZON_MIDDLEWARE_USER        | The name of the 'admin' middleware user. This will get stored as a medius account                                   |
| HORIZON_MIDDLEWARE_PASSWORD    | The password for the middleware admin user                                                                          |
| HORIZON_APP_ID                 | The APP id to host. Can be a comma separated list                                                                   |

Set up the configs in the `horizon-server/docker/` folder. You can change the ports, `PublicIpOverride` (if developing locally), or MUIS information if using MUIS. Don't forget to set the app ids in the `dme.config`!

Start running the docker containers in the right order. First start the `horizon-server-database` docker container by running `horizon-server-database/run.sh`. By default this will start running it in the background, and save the db info into the `horizon-server-database` folder.

Next, start the middleware. Start the container by running `horizon-server-database-middleware/run.sh`. Again this will by default run in the background.

Now you can start running the server. Run the horizon-server container by running `horizon-server/docker/run.sh`.

### Using plugins and patches
The following shows an example of how to use the patch and plugin for UYA. Download two more repositories:
- [UYA Patch](https://github.com/Horizon-Private-Server/horizon-uya-patch)
- [UYA Plugin](https://github.com/Horizon-Private-Server/horizon-uya-plugin)

Set your directory structure like so:
```
├── horizon/
│   └── horizon-server/
│   └── horizon-server-database/
│   └── horizon-server-database-middleware/
│   └── horizon-uya-plugin/
│   └── horizon-uya-patch/
```
Start the database and middleware containers like normal.

Build the patch by running `horizon-uya-patch/build.sh`. This will build the patch, and copy the binaries into the `horizon-server/docker/patch/` folder.

Build the plugin by running `horizon-uya-plugin/build.sh`. This will compile it and copy the binaries into `horizon-server/docker/medius_plugins` and `horizon-server/docker/dme_plugins`.

Now that the patch and plugins are in place, you can run the `horizon-server` container.

### Debugging the db
```
sqlcmd -S ${SERVER_IP},1433 -U sa -P "${YOUR_PASSWORD}"
```

### Misc
The default configs in the `horizon-server/docker/` folder were last generated 08/29/2022.


### Instructions from Dan on Setup
You'll want to setup an instance of sql server. I recommend using docker for this. I use docker for windows and then inside it run the db server. Alternatively you can run it on windows as a service. There are tutorials for that online as well. Here's one for running it in docker:
https://docs.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?view=sql-server-ver16&pivots=cs1-bash
Note that the tutorial is for linux but the same docker commands will work inside of powershell on windows 10 (just remove the sudo from each command)

If you use docker you'll want to mount the database's data, log, and secrets folders to somewhere on your host machine so that the data persists even after the docker instance stops
I've also updated the master branch of https://github.com/Horizon-Private-Server/horizon-server and the database middleware with the latest that I have. I recommend pulling those. The only difference you need to worry about with those changes is the config files have been renamed to medius.json, dme.json, and muis.json for their respective server components. Previously they were all named config.json. Also the dme server needs a copy of the db.config.json

Once you have the database setup you'll want to download and install SSMS (sql server management studio) and connect to the db over 127.0.0.1,1433 with whatever credentials you setup when you created it. Once connected you'll want to run the CREATE_DATABASE script and then the CREATE_TABLES script from here: https://github.com/Horizon-Private-Server/horizon-server-database-middleware/tree/master/scripts

Then inside of the middleware you'll want to edit the launchSettings.json such that you have the following environment variables setup with the correct username and password for the database:

```
"Horizon.Database": {
  "commandName": "Project",
  "launchBrowser": true,
  "environmentVariables": {
    "DB_USER": "",
    "DB_NAME": "Medius_Database",
    "DB_PASSWORD": "",
    "DB_SERVER": "127.0.0.1,1433",
    "ASPNETCORE_ENVIRONMENT": "Development"
  },
  "applicationUrl": "https://localhost:5001;http://localhost:5000"
}
```

For a production instance you'll also want to setup a new user that only has permissions to access the Medius_Database db and only has permissions to create/update/insert/delete/select
and use that account for DB_USER and DB_PASSWORD environment vars
and finally you'll want to create a user inside of the swagger api for the user that the medius and dme servers will authenticate as. I use the same user for both but you can create different users for each server instance. Launch the middleware and go to
```
/swagger/index.html
```

From there go to /Account/createAccount and begin a request. You'll want to set it up like so but with whatever fields for the AccountName and AccountPassword. Then hit execute to create the account. Inside of db.config.json set the username to the username you just entered and the password to the password you just entered

```
{
  "AccountName": "",
  "AccountPassword": "",
  "MachineId": NULL,
  "MediusStats": NULL,
  "AppId": 0,
  "PasswordPreHashed": false
}
```
You'll also want to give the user the
```
database
```
role so that they can do all the things that the medius/dme server need to do. To do that open SSMS and go to the KEYS.roles table, right click and select SELECT TOP 1000 ROWS. You should see a row named database with an id. The id is probably 1 but just make note of it. Do the same thing for ACCOUNTS.account except look for the account id you just made. Then right click ACCOUNTS.user_role and select EDIT TOP 200 ROWS. From there enter the account id, role id, and then GETDATE() for the create_dt and from_dt tables. Leave to_dt null.


Also for each app id you want to support you'll have to create an entry in dim_app_ids. If you want to have two different app ids share the same data/games/etc (cross region) then create an app group and assign the respective app ids to that group. This is how I have R&C setup

Finally once everything is setup and the medius/dme servers connect to the middleware for the first time you'll see the server_settings table be populated with rows. You can edit these per app id to configure many of the settings that once resided inside of config.json

AccountIdWhitelist is a comma-delimited list of account ids. All the TextFilter* are regular expressions where any text that doesn't match the regex will be rejected. More comments on the settings are in the Server.Medius/Config/AppSettings.cs and Server.Dme/Config/AppSettings.cs
