using System;
using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Collections.Generic;

namespace Service
{
    class Program
    {
        private static EventHubClient s_eventHubClient;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Read temperature messages. Ctrl-C to exit.\n");

            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("conf.json", true, true)
                .Build();

            var connectionString = new EventHubsConnectionStringBuilder(new Uri(config["EventHubsCompatibleEndpoint"]), config["EventHubsCompatiblePath"], config["IotHubSasKeyName"], config["IotHubSasKey"]);
            s_eventHubClient = EventHubClient.CreateFromConnectionString(connectionString.ToString());

            var runtimeInfo = await s_eventHubClient.GetRuntimeInformationAsync();
            var d2cPartitions = runtimeInfo.PartitionIds;

            CancellationTokenSource cts = new CancellationTokenSource();

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            var tasks = new List<Task>();
            foreach (string partition in d2cPartitions)
            {
                tasks.Add(ReceiveMessagesFromDeviceAsync(partition, cts.Token));
            }

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
                var events = await eventHubReceiver.ReceiveAsync(100);

                if (events == null) continue;

                foreach(EventData eventData in events)
                {
                    string data = Encoding.UTF8.GetString(eventData.Body.Array);
                    Console.WriteLine("Message received on partition {0}:", partition);

                    Console.WriteLine("  {0}:", data);
                    Console.WriteLine("Application properties (set by device):");
                    foreach (var prop in eventData.Properties)
                    {
                        Console.WriteLine("  {0}: {1}", prop.Key, prop.Value);
                    }
                    Console.WriteLine("System properties (set by IoT Hub):");
                    foreach (var prop in eventData.SystemProperties)
                    {
                        Console.WriteLine("  {0}: {1}", prop.Key, prop.Value);
                    }
                }
            }
        }
    }
}
