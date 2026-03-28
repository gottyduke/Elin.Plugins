## Expanded Moongate Project(WIP)

`ExpandedMoongate` adds support for external moongate servers and an optional rating/comment system for Elin.

It's built with 3 modules working together:

- `ExpandedMoongate`: BepInEx mod that runs in Elin as UI and client.
- `ExpandedMoongate.CFGateway`: API gateway that exposes the server to clients.
- `ExpandedMoongate.GCPWorker`: Google Cloud Job that updates maps from Elin moongate server to this one.

## TODO

- Map ID deduplication - this requires Elin base code change to accept Steam User ID instead of IP address.
- Moderation API.

## Server Spec

See `API` folder for providing custom server endpoints.