using System;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.FileExtensions;
using Microsoft.Extensions.Configuration.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Service
{
    class Program
    {
        private static EventHubClient s_eventHubClient;

        private static ServiceClient serviceClient;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Read temperature messages. Ctrl-C to exit.\n");

            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("conf.json", true, true)
                .Build();

            serviceClient = ServiceClient.CreateFromConnectionString(config["IotHubConnectionString"]);

            var connectionString = new EventHubsConnectionStringBuilder(new Uri(config["EventHubsCompatibleEndpoint"]), config["EventHubsCompatiblePath"], config["IotHubSasKeyName"], config["IotHubSasKey"]);
            s_eventHubClient = EventHubClient.CreateFromConnectionString(connectionString.ToString());

            var runtimeInfo = await s_eventHubClient.GetRuntimeInformationAsync();
            var d2cPartitions = runtimeInfo.PartitionIds;

            CancellationTokenSource cts = new CancellationTokenSource();

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };

            var tasks = new List<Task>();
            foreach (string partition in d2cPartitions)
            {
                tasks.Add(ReceiveMessagesFromDeviceAsync(partition, cts.Token));
            }

            tasks.Add(ReceiveFeedbackAsync(cts.Token));

            Task.WaitAll(tasks.ToArray());

            Console.ReadLine();
        }

        private static async Task ReceiveMessagesFromDeviceAsync(string partition, CancellationToken ct)
        {
            var eventHubReceiver = s_eventHubClient.CreateReceiver("$Default", partition, EventPosition.FromEnqueuedTime(DateTime.Now));
            Console.WriteLine("Create receiver on partition: " + partition);
            while (true)
            {
                if (ct.IsCancellationRequested) break;
                Console.WriteLine("Listening for messages on: " + partition);
                var events = await eventHubReceiver.ReceiveAsync(100);

                if (events == null) continue;

                foreach(EventData eventData in events)
                {
                    string data = Encoding.UTF8.GetString(eventData.Body.Array);
                    Console.WriteLine("Message received on partition {0}:", partition);

                    var temperature = double.Parse(data, System.Globalization.CultureInfo.InvariantCulture);
                    var deviceId = eventData.SystemProperties["iothub-connection-device-id"].ToString();
                    Console.WriteLine($"Device: {deviceId} - data: {temperature}");

                    if (temperature > 23)
                        await SendCloudToDeviceMessageAsync(deviceId, false);
                    else if (temperature < 19)
                        await SendCloudToDeviceMessageAsync(deviceId, true);
                }
            }
        }

        private static async Task SendCloudToDeviceMessageAsync(string deviceId, bool turnOn)
        {
            var message = turnOn ? "turn on" : "turn off";
            var commandMessage = new Message(Encoding.ASCII.GetBytes(message));
            commandMessage.Ack = DeliveryAcknowledgement.Full;
            commandMessage.MessageId = Guid.NewGuid().ToString();
            await serviceClient.SendAsync(deviceId, commandMessage);
            Console.WriteLine($"Message \"{message}\" sent to device {deviceId} with Id: {commandMessage.MessageId}");
        }

        private async static Task ReceiveFeedbackAsync(CancellationToken ct)
        {
            var feedbackReceiver = serviceClient.GetFeedbackReceiver();

            while (true)
            {
                if (ct.IsCancellationRequested) break;

                var feedbackBatch = await feedbackReceiver.ReceiveAsync();
                if (feedbackBatch == null) continue;

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Received feedback: {0}", 
                    string.Join(", ", feedbackBatch.Records.Select(f => $"Message Id: {f.OriginalMessageId} status: {f.StatusCode}")));
                Console.ResetColor();

                await feedbackReceiver.CompleteAsync(feedbackBatch);
            }
        }
    }
}
