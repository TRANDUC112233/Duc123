using System;
using System.Text.Json;
using System.Threading.Tasks;
using MQTT;

namespace LoginApp.Service
{
    public class MQTTControlService
    {
        private readonly MQTTDeviceClient mqttClient;
        private readonly string controlTopic;

        public event Action<string>? OnCommandSent;
        public event Action<string>? OnError;

        public bool IsConnected { get; private set; } = false;

        public MQTTControlService(
            string userId = "1", // Luôn là "1" cho khớp với espID trên phần cứng
            string brokerAddress = "broker.emqx.io",
            int brokerPort = 8883,
            string clientId = "maui-control-client")
        {
            // Kênh điều khiển luôn là "1/device" cho khớp với String espID="1"
            controlTopic = "1/device";

            mqttClient = new MQTTDeviceClient(
                brokerAddress: brokerAddress,
                brokerPort: brokerPort,
                topic: controlTopic,
                clientId: clientId
            );
        }

        public async Task StartAsync(string? username = null, string? password = null)
        {
            try
            {
                await mqttClient.ConnectAsync(username, password);
                IsConnected = true;
                Console.WriteLine("[MQTTControlService] Đã kết nối tới MQTT broker.");
            }
            catch (Exception ex)
            {
                IsConnected = false;
                OnError?.Invoke($"Lỗi khi kết nối MQTT: {ex.Message}");
                throw;
            }
        }

        public async Task StopAsync()
        {
            try
            {
                if (IsConnected)
                {
                    await mqttClient.DisconnectAsync();
                    IsConnected = false;
                    Console.WriteLine("[MQTTControlService] Đã ngắt kết nối MQTT broker.");
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Lỗi khi ngắt kết nối MQTT: {ex.Message}");
            }
        }

        // Gửi lệnh đến thiết bị (pump, light, valve)
        public async Task SendDeviceCommandAsync(string cmd, bool state)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Chưa kết nối MQTT broker.");

            try
            {
                var payload = new
                {
                    cmd = cmd.ToUpper(), // ESP32 dùng "PUMP", "LIGHT"
                    value = state ? 1 : 0
                };
                string json = JsonSerializer.Serialize(payload);

                // Đúng hàm publish: sử dụng mqttClient.SendCommandAsync (đã có trong MQTTDeviceClient)
                // Lưu ý: SendCommandAsync(string cmd, int value) sẽ tự serialize đúng JSON và gửi lên topic controlTopic
                await mqttClient.SendCommandAsync(cmd.ToUpper(), state ? 1 : 0);

                Console.WriteLine($"[MQTTControlService] Đã gửi lệnh: {json}");
                OnCommandSent?.Invoke(json);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Lỗi khi gửi lệnh MQTT: {ex.Message}");
            }
        }
    }
}