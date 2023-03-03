using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCGeoRefCheckerGUI.Messaging
{
    public class Subscription : IDisposable
    {
        Action removeMethod;

        public Subscription(Action removeMethod)
        {
            this.removeMethod = removeMethod;
        }

        public void Dispose()
        {
            if (this.removeMethod != null)
            {
                this.removeMethod();
            }
        }
    }
}
