using System;
using System.Threading.Tasks;
using MQTT;
using LoginApp; // Để dùng SensorDataEventArgs

namespace LoginApp.Service
{
    public class MQTTService
    {
        private readonly MQTTDeviceClient client;

        // Sự kiện truyền dữ liệu cảm biến: nhiệt độ, độ ẩm, mực nước
        public event EventHandler<SensorDataEventArgs>? SensorDataReceived;
        public event Action<string>? OnError;

        public bool IsConnected { get; private set; } = false;

        public MQTTService(
            string userId,
            string brokerAddress = "broker.emqx.io",
            int brokerPort = 8883,
            string clientId = "maui-client")
        {
            // Kênh cảm biến: "UserId/Sensor"
            string topic = $"{Globals.GlobalUserId}/Sensor";

            client = new MQTTDeviceClient(
                brokerAddress: brokerAddress,
                brokerPort: brokerPort,
                topic: topic,
                clientId: clientId
            );

            // Khi có dữ liệu cảm biến từ MQTT, raise event cho UI
            client.OnSensorDataReceived += (temp, hum, waterlevel) =>
            {
                SensorDataReceived?.Invoke(this, new SensorDataEventArgs
                {
                    Temperature = temp,
                    Humidity = hum,
                    WaterLevel = waterlevel
                });
            };
        }

        public async Task StartAsync(string? username = null, string? password = null)
        {
            try
            {
                await client.ConnectAsync(username, password);
                IsConnected = true;
                Console.WriteLine("[MQTTService] Đã kết nối tới MQTT broker.");
            }
            catch (Exception ex)
            {
                IsConnected = false;
                OnError?.Invoke($"Lỗi khi kết nối MQTT: {ex.Message}");
                Console.WriteLine($"[MQTTService] Lỗi khi kết nối MQTT: {ex.Message}");
                throw;
            }
        }

        public async Task StopAsync()
        {
            try
            {
                if (IsConnected)
                {
                    await client.DisconnectAsync();
                    IsConnected = false;
                    Console.WriteLine("[MQTTService] Đã ngắt kết nối MQTT broker.");
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Lỗi khi ngắt kết nối MQTT: {ex.Message}");
                Console.WriteLine($"[MQTTService] Lỗi khi ngắt kết nối MQTT: {ex.Message}");
            }
        }
    }
}