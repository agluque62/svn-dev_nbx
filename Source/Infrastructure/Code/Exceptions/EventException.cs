using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U5ki.Infrastructure.Exceptions
{
    public class EventException : Exception
    {
        public EventException() : base("Event Handler has raised an event general exception.") { }
        public EventException(string message) : base("Event Handler has raised an event exception: " + message) { }
        public EventException(Exception ex) : base("Event Handler has raised an event exception: " + ex.Message) { }
    }
}
