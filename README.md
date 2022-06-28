# Getting started

This sample programmatically demonstrates this functionality:
- Sending a message to the IoT Hub
- Handling a Desired Property change
- Handing a Direct Method call
- Accessing the Device Twin

This sample has a dependency on the IoT Hub Device SDK only:

```powershell
dotnet add package Microsoft.Azure.Devices.Client --version 1.41.0
```

Azure IoT SDK for .NET GH repo: https://github.com/Azure/azure-iot-sdk-csharp

## Create env var

```powershell
[System.Environment]::SetEnvironmentVariable('DEVICE_CONNECTION_STRING','HostName=<hub-name>.azure-devices.net;DeviceId=<device-id>;SharedAccessKey=<key>')
```

👆 If you don't set this env var, you'll receive this null reference exception: `The DEVICE_CONNECTION_STRING environment variable is missing`

## Add desired property to device twin

```json
"properties": {
    "desired": {
        "refeshRateInSeconds": 60,
```

OR

```powershell
az iot hub device-twin update -n <hub-name> -d <device-id> --desired '{"refeshRateInSeconds":5}'
```

## To execute a direct method (UpdateFirmware)

```powershell
az iot hub invoke-device-method -n <hub-name> --device-id <device-name> --method-name "UpdateFirmware" --method-payload "{}"
```

## To stream events

```powershell
az iot hub monitor-events --hub-name <hub-name>
```

## To run

```powershell
dotnet restore
dotnet run
```

```
IoT Hub C# Simulated Cave Device. Ctrl-C to exit.

28/06/2022 14:23:32 > Sending message: {"temperature":28.673285191141417,"humidity":78.94513150713539}
desired property change:
{"chiller-water":{"temperature":"66","pressure":28},"prop1":1,"refeshRateInSeconds":30,"$version":15}
Sending current time as reported property
New refresh rate is 30, previous refresh rate was 60
28/06/2022 14:24:32 > Sending message: {"temperature":22.53495102505084,"humidity":63.31795092756889}
method UpdateFirmware handled with body {}
28/06/2022 14:25:02 > Sending message: {"temperature":21.41224351155128,"humidity":73.31948665161649}
desired property change:
{"chiller-water":{"temperature":"66","pressure":28},"prop1":1,"refeshRateInSeconds":60,"$version":16}
Sending current time as reported property
New refresh rate is 60, previous refresh rate was 30
```