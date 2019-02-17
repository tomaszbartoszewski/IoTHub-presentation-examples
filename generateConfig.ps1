$iotHubName=$args[0]
$deviceName=$args[1]

$deviceConnectionString = az iot hub device-identity show-connection-string --device-id $deviceName --hub-name $iotHubName | ConvertFrom-Json

$eventHubsCompatibleEndpoint = az iot hub show --query properties.eventHubEndpoints.events.endpoint --name $iotHubName

$eventHubsCompatiblePath = az iot hub show --query properties.eventHubEndpoints.events.path --name $iotHubName

$iotHubSasKey = az iot hub policy show --name iothubowner --query primaryKey --hub-name $iotHubName

$iotHubConnectionString = az iot hub show-connection-string --hub-name $iotHubName | ConvertFrom-Json

$serviceConfiguration = @{
EventHubsCompatibleEndpoint=$eventHubsCompatibleEndpoint.Trim('"');
EventHubsCompatiblePath=$eventHubsCompatiblePath.Trim('"');
IotHubSasKeyName="iothubowner";
IotHubSasKey=$iotHubSasKey.Trim('"')
IotHubConnectionString=$iotHubConnectionString.cs
}

$serviceConfiguration | ConvertTo-Json | Out-File Service\conf.json


$deviceConfiguration = @{
DeviceConnectionString=$deviceConnectionString.cs;
}

$deviceConfiguration | ConvertTo-Json | Out-File Device\conf.json