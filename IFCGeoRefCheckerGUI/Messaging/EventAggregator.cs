using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCGeoRefCheckerGUI.Messaging
{
    public class EventAggregator : IEventAggregator
    {
        object locker = new object();
        List<(Type eventType, Delegate methodToCall)> eventRegistrations = new List<(Type eventType, Delegate methodToCall)>();

        public Subscription Subscribe<T>(Action<T> action)
        {
            if (action != null)
            {
                lock (locker)
                {
                    this.eventRegistrations.Add((typeof(T), action));
                    return new Subscription(() =>
                    {
                        this.eventRegistrations.Remove((typeof(T), action));
                    });
                }
            }
            return new Subscription(() => { });
        }

        public void Publish<T>(T data)
        {
            List<(Type type, Delegate methodToCall)>? regs = null;
            lock (locker)
            {
                regs = new List<(Type type, Delegate methodToCall)>(eventRegistrations);
            }
            foreach (var item in regs) 
            {
                if (item.type == typeof(T))
                {
                    ((Action<T>)item.methodToCall)(data);
                }
            }
        }
    }
}
