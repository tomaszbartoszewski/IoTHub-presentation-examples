using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.FileExtensions;
using Microsoft.Extensions.Configuration.Json;
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

            await SendRandomTemperatureAsync();
        }

        private static async Task SendRandomTemperatureAsync()
        {
            double minTemperature = 20;
            Random rand = new Random();

            while (true)
            {
                double currentTemperature = minTemperature + rand.NextDouble() * 15;
                var messageString = JsonConvert.SerializeObject(currentTemperature);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                await s_deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

                await Task.Delay(1000);
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
