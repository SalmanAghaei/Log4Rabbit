using RabbitMQ.Client;
using System.Collections.Generic;

namespace Log4Rabbit.Send.RabbitClient
{
    public class SSLConfig : SslOption { }
    public class RabbitMqClientSetting
    {

        public string HostName { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Exchange { get; set; } = string.Empty;

        public string RouteKey { get; set; } = string.Empty;

        public int Port { get; set; }

        public string VHost { get; set; } = string.Empty;

        public SSLConfig SslOption { get; set; }

    }
}
