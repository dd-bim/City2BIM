namespace BIMGISInteropLibs.Logging
{
    public class LogPair
    {
            private LogType type;
            private string message;

            public LogType Type { get => type; set => type = value; }
            public string Message { get => message; set => message = value; }

            public LogPair(LogType type, string message)
            {
                this.type = type;
                this.message = message;
            }
    }
    public enum LogType { error, info, warning, verbose, debug }
}
