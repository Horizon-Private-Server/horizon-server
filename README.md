# horizon-server
This repository contains a Medius server emulator that works for a variety of games. Originally built to revive the Ratchet: Deadlocked PS2 online servers.

## Running with Docker
1. Set up the configs in the `docker/` container. You can change the ports, `PublicIpOverride` (if developing locally), or MUIS information if using MUIS
2. Execute `build.sh` to build the image
3. Check the ports in `run.sh` to ensure you're exposing the same ports that are set in the config
4. Run `run.sh` (change `-it` to `-d` to run it in the background)

Generally, for local development, change:
- `dme.json`: `PublicIpOverride`, `ApplicationIds`
- `medius.json`: `PublicIpOverride`, `ApplicationIds`, `NATIp`, ports if MUIS is not enabled and MAS default is not 10075
- `muis.json`: `Universes`

The default configs in the `docker/` folder were last generated 13 Feb 2022.
