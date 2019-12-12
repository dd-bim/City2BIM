using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace City2BIM.Logging
{
    public class LogPair
    {
            private LogWriter.LogType type;
            private string message;

            public LogWriter.LogType Type { get => type; set => type = value; }
            public string Message { get => message; set => message = value; }

            public LogPair(LogWriter.LogType type, string message)
            {
                this.type = type;
                this.message = message;
            }
    }
}
