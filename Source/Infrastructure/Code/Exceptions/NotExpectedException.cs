using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U5ki.Infrastructure.Resources;

namespace U5ki.Infrastructure.Exceptions
{
    public class NotExpectedException : Exception
    {

        public NotExpectedException() : base(Errors.NotExpectedException) { }
        public NotExpectedException(string details) : base(Errors.NotExpectedException + ": " + details) { }

    }
}
