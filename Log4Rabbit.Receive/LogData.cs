namespace Log4Rabbit.Receive
{
    public class LogData
    {
        public string Timestamp { get; set; }
        public string Level { get; set; }
        public string MessageTemplate { get; set; }
        public Properties Properties { get; set; }

        public string Exception { get; set; }

    }

    public class Properties
    {

        public string CommandType { get; set; }
        public string RouteData { get; set; }
        public string MethodInfo { get; set; }
        public string Controller { get; set; }
        public string AssemblyName { get; set; }
        public EventId EventId { get; set; }
        public string SourceContext { get; set; }
        public string ActionId { get; set; }
        public string ActionName { get; set; }
        public string RequestId { get; set; }
        public string RequestPath { get; set; }
        public string ConnectionId { get; set; }
        public string ApplicationContext { get; set; }
        public string MachineName { get; set; }

    }

    public class EventId
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
