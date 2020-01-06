# Event Sorcery

Features a configurable toolset based on the [Event Sourcing](https://martinfowler.com/eaaDev/EventSourcing.html) design pattern.

## Prerequisites

To build this application, the following prerequisites need to be in place:

* .NET Core SDK 3.1

The following prerequisites are optional:

* make

### Ubuntu

On ubuntu the prerequisites can be installed with `apt` using the following command:

`apt install make dotnet-sdk-3.1`

It may be necessary to follow [this guide](https://docs.microsoft.com/en-us/dotnet/core/install/linux-package-manager-ubuntu-1904) to wire .NET Core into `apt`.

## Run

```sh
make run
```

This target takes by default the configuration files stored at `src/deploy/`. To use another configuration, supply the `CONFIGURATION_PATH` variable:

```sh
make run CONFIGURATION_PATH=/etc/event-sorcery
```

## Build

```sh
make build
```

## Installation

The application includes installation

`make install`

### Remote deploy

The following shorthand builds against `arm` and upgrades an existing installation, affecting only binary files and the configuration template, conneting to the remote host `remote` using `ssh`:

```sh
$ make clean build && make deploy-upgrade ARCH=linux-arm HOST=remote
```

## Configuration

The application looks for configurations at the following paths:

* `/etc/event-sorcery/config.json`
* `/etc/event-sorcery/conf.d/*.json`

It sorts then the files by their names in ascending order and inspects the files in this order. Configuration files loaded later override any configuration configured in files loaded earlier. Therefore, the base configuration file `config.json` should not be modified. Instead the file should be copied to `conf.d/` and edited as needed. It is also wise to separate the customized configuration files by their logical operation.

### Example: conf.d/01-mqtt-broker.json

The following example configuration file `conf.d/01-mqtt-broker.json` outlines how the MQTT broker could be configured with SSL and authentication:

```json
{
    "Mqtt": {
        "Host": "some.ssl.host",
        "Port": 8883,
        "Ssl": {
            "Enable": true,
            "AllowUntrustedCertificate": false
        },
        "Auth": {
            "Enable": true,
            "Username": "unknown",
            "Password": "secret"
        }
    }
}
```
