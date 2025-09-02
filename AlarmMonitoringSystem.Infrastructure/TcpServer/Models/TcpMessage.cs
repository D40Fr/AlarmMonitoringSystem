namespace AlarmMonitoringSystem.Infrastructure.TcpServer.Models
{
    public class TcpMessage
    {
        public string ClientId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        public string MessageType { get; set; } = "ALARM"; // ALARM, HEARTBEAT, etc.
        public int Length { get; set; }
        public string? IpAddress { get; set; }
        public int? Port { get; set; }

        public bool IsEmpty => string.IsNullOrWhiteSpace(Content);
        public bool IsValidJson
        {
            get
            {
                try
                {
                    System.Text.Json.JsonDocument.Parse(Content);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static TcpMessage Create(string clientId, string content, string? ipAddress = null, int? port = null)
        {
            return new TcpMessage
            {
                ClientId = clientId,
                Content = content,
                Length = content?.Length ?? 0,
                IpAddress = ipAddress,
                Port = port,
                ReceivedAt = DateTime.UtcNow,
                MessageType = DetermineMessageType(content)
            };
        }

        private static string DetermineMessageType(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return "EMPTY";

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("type", out var typeProperty))
                {
                    var type = typeProperty.GetString()?.ToUpperInvariant();
                    return type switch
                    {
                        "HEARTBEAT" => "HEARTBEAT",
                        "PING" => "HEARTBEAT",
                        _ => "ALARM"
                    };
                }

                return "ALARM";
            }
            catch
            {
                return "INVALID";
            }
        }
    }
}