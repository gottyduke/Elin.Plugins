## Expanded Moongate Project(WIP)

![](https://img.shields.io/endpoint?url=https://api.exmoongate.elin-modding.net/badge/maps)

`ExpandedMoongate` adds support for external moongate servers and an optional rating/comment system for Elin.

It's built with 3 modules working together:

- `ExpandedMoongate`: BepInEx mod that runs in Elin as UI and client.
- `ExpandedMoongate.CFGateway`: API gateway that exposes the server to clients.
- `ExpandedMoongate.GCPWorker`: Google Cloud Job that updates maps from Elin moongate server to this one.

## TODO

- Map ID deduplication - this requires Elin base code change to accept Steam User ID instead of IP address.
- Moderation API.
- Map Preview.

## Server Spec

See [Expanded Moongate Server API](https://docs.exmoongate.elin-modding.net/)