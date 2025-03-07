using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XerLoader
{
    public class InitializingEventArgs(XerElement xerElement, TimeSpan time) : InitializeEventArgs(time)
    {
        public XerElement XerElement { get; private set; } = xerElement;

    }

    public class InitializeEventArgs(TimeSpan time) : EventArgs
    {
        public TimeSpan Elepsed { get; private set; } = time;
    }

}
