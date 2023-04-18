using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCGeoRefCheckerGUI.Messaging
{
    public interface IEventAggregator
    {
        void Publish<T>(T data);
        Subscription Subscribe<T>(Action<T> action);
    }
}
