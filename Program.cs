/*
 * device twins: https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-twin-getstarted
 */

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace IotBasicExample
{
    class Program
    {        
        // Contains methods that a device can use to send messages to and receive from an IoT Hub.
        private static DeviceClient deviceClient;

        // The device connection string to authenticate the device with your IoT hub.        
        private static string connectionString = String.Empty;

        private static int messageRefreshRateInSeconds = 60;

        private static bool defaultGotFromDeviceTwin = false;
                
        private static async Task Main(string[] args)
        {
            Console.WriteLine("IoT Hub C# Simulated Cave Device. Ctrl-C to exit.\n");

            // get device connection string from env var
            connectionString = System.Environment.GetEnvironmentVariable("DEVICE_CONNECTION_STRING") ?? throw new NullReferenceException("The DEVICE_CONNECTION_STRING environment variable is missing");
            
            // Connect to the IoT hub using the MQTT protocol
            deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);
            
            // setup desired prop callback
            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, null);

            // get refresh Rate from device twin
            await deviceClient.GetTwinAsync().ContinueWith((x)=>
            {
                if (x.IsCompletedSuccessfully)
                {
                    SetRefreshRate((int)x.Result.Properties.Desired["refeshRateInSeconds"]);
                    defaultGotFromDeviceTwin = true;
                }                 
            });

            // handle direct method call
            await deviceClient.SetMethodHandlerAsync("UpdateFirmware", OnMethodHandler, null);

            SendDeviceToCloudMessagesAsync();
            Console.ReadLine();            
        }

        private static Task<MethodResponse> OnMethodHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"method {methodRequest.Name} handled with body {methodRequest.DataAsJson}");
            return Task.FromResult(new MethodResponse(new byte[0], 200));
        }

        private static void SetRefreshRate(int seconds)
        {
            if (seconds != messageRefreshRateInSeconds)
            {
                Console.WriteLine($"New refresh rate is {seconds}, previous refresh rate was {messageRefreshRateInSeconds}");
                messageRefreshRateInSeconds = seconds;
            }
        }

        private static async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
        {
            Console.WriteLine("desired property change:");
            Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));
            Console.WriteLine("Sending current time as reported property");
            TwinCollection reportedProperties = new TwinCollection
            {
                ["DateTimeLastDesiredPropertyChangeReceived"] = DateTime.Now
            };

            if (desiredProperties["refeshRateInSeconds"] !=null)
            {
                SetRefreshRate((int)desiredProperties["refeshRateInSeconds"]);                
            };

            //if (int.TryParse(desiredProperties["refeshRateInSeconds"], out int desiredValue))
            //{
            //    if (desiredValue != MessageRefreshRateInSeconds)
            //    {
            //        Console.WriteLine($"New refresh rate is {desiredValue}, previous refresh rate was {MessageRefreshRateInSeconds}");
            //        MessageRefreshRateInSeconds = desiredValue;                    
            //    }                
            //}

            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
        }

        public static async void ReportConnectivity()
        {
            try
            {
                Console.WriteLine("Sending connectivity data as reported property");

                TwinCollection reportedProperties, connectivity;
                reportedProperties = new TwinCollection();
                connectivity = new TwinCollection();
                connectivity["type"] = "cellular";
                reportedProperties["connectivity"] = connectivity;
                await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }
                
        private static async void SendDeviceToCloudMessagesAsync()
        {

            if (!defaultGotFromDeviceTwin) return;

            // Create an instance of our sensor 
            var sensor = new EnvironmentSensor();

            while (true)
            {
                // read data from the sensor
                var currentTemperature = sensor.ReadTemperature();
                var currentHumidity = sensor.ReadHumidity();

                var messageString = CreateMessageString(currentTemperature, currentHumidity);

                // create a byte array from the message string using ASCII encoding
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                // Add a custom application property to the message.
                // An IoT hub can filter on these properties without access to the message body.
                message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

                // Send the telemetry message
                await deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

                await Task.Delay(messageRefreshRateInSeconds * 1000);
            }
        }
                
        private static string CreateMessageString(double temperature, double humidity)
        {
            // Create an anonymous object that matches the data structure we wish to send
            var telemetryDataPoint = new
            {
                temperature = temperature,
                humidity = humidity
            };

            // Create a JSON string from the anonymous object
            return JsonConvert.SerializeObject(telemetryDataPoint);
        }
    }

        
    /// <summary>
    /// This class represents a sensor 
    /// real-world sensors would contain code to initialize
    /// the device or devices and maintain internal state
    /// a real-world example can be found here: https://bit.ly/IoT-BME280
    /// </summary>
    internal class EnvironmentSensor
    {
        // Initial telemetry values
        double minTemperature = 20;
        double minHumidity = 60;
        Random rand = new Random();

        internal EnvironmentSensor()
        {
            // device initialization could occur here
        }

        internal double ReadTemperature()
        {
            return minTemperature + rand.NextDouble() * 15;
        }

        internal double ReadHumidity()
        {
            return minHumidity + rand.NextDouble() * 20;
        }
    }
}