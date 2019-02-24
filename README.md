# IoT Hub examples

To run this examples you will need Azure IoT Hub. You can check this tutorial for creating IoT Hub and connecting to it. [Tutorial](https://tomaszbartoszewski.github.io/IoTHub-tutorial/)

To generate config you will need Azure Cli. Check this website for installing it
[Azure Cli](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)

Then run this command in powershell:

```
az extension add --name azure-cli-iot-ext
```

Now you can run script, first parameter is name of IoT Hub, second is your device Id:

```
.\generateConfig.ps1 'first-try' 'bedroom'
```

If you don't want to run script, you can generate configs manually. For Device you will need this config.json (you can find device connection string on Device details page):

```
{
    "DeviceConnectionString":  ""
}
```

Place it inside both Device and Device_Processing_Commands directories.

Service require more configuration, you can find it on Built-in endpoints in box Event Hub - compatible endpoint. For IotHubConnectionString go to Shared access policies and select iothubowner (only for this demo, be sensible when running on production):

```
{
    "EventHubsCompatibleEndpoint":  "",
    "IotHubSasKeyName":  "",
    "EventHubsCompatiblePath":  "",
    "IotHubConnectionString":  "",
    "IotHubSasKey":  ""
}
```

Place it inside all 4 Service directories.

Now we can run code.

Every example should work after running

```
dotnet restore; dotnet build; dotnet run
```

### Device directory

It will run device simulator which sends random temperature every second. You can turn on monitoring in Azure IoT Hub Devices extension which we used before.

### Service directory

Continue running Device code. We can now start service processing telemetry. It will display received values on console.

### Service_Just_Id_And_Value directory

It's changed version to display only device Id and it's temperature.

### Service_Send_Command directory

Now it will read temperatures from IoT Hub and send commands to device. Because our Device code can't process commands, it's time to run new code for simulating device.

### Device_Processing_Commands directory

It will now change it's behaviour depending if last command was turn on or off. Turned off it will lower it's temperature with every step, if on it will increase it.

### Service_Send_Command_Ack directory

Our service was not aware if message was delivered correctly, if you would like to get information about messages run this example. You can see it tries to collect feedback in batches instead of getting it one by one.
