using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Device
{
    class Program
    {
        private static DeviceClient s_deviceClient;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Simulated device. Ctrl-C to exit.\n");

            var deviceConnectionString = DeviceConnectionString();
            s_deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);

            CancellationTokenSource cts = new CancellationTokenSource();

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            var tasks = new List<Task>
            {
                SimulateHeaterAsync(cts.Token),
                ReceiveCommandsAsync(cts.Token)
            };

            await Task.WhenAll(tasks.ToArray());
        }

        private static bool isOn = false;

        private static async Task SimulateHeaterAsync(CancellationToken ct)
        {
            Random rand = new Random();
            double temperature = 18 + rand.NextDouble() * 3;
            while (true)
            {
                try
                {
                    if (ct.IsCancellationRequested) break;

                    if (isOn && temperature < 26)
                        temperature += 0.5;
                    if (!isOn && temperature > 13)
                        temperature -= 0.5;

                    Console.WriteLine(temperature);
                    var messageString = JsonConvert.SerializeObject(temperature);
                    var message = new Message(Encoding.ASCII.GetBytes(messageString));

                    await s_deviceClient.SendEventAsync(message);
                    Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

                    await Task.Delay(1000);
                }
                catch {}
            }
        }

        private static async Task ReceiveCommandsAsync(CancellationToken ct)
        {
            while (true)
            {
                try
                {
                    if (ct.IsCancellationRequested) break;

                    Message receivedMessage = await s_deviceClient.ReceiveAsync();
                    if (receivedMessage == null) continue;

                    var command = Encoding.ASCII.GetString(receivedMessage.GetBytes());

                    if (string.Equals(command, "turn on", StringComparison.InvariantCultureIgnoreCase))
                        isOn = true;
                    else if (string.Equals(command, "turn off", StringComparison.InvariantCultureIgnoreCase))
                        isOn = false;

                    Console.WriteLine($"Received command: {command}");

                    await s_deviceClient.CompleteAsync(receivedMessage);
                }
                catch {}
            }
        }

        static string DeviceConnectionString()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("conf.json", true, true)
                .Build();
            return config["DeviceConnectionString"];
        }
    }
}
