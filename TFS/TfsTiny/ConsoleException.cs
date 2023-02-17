using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inedo.TFS
{
    internal sealed class ConsoleException : Exception
    {
        public ConsoleException(string message) : base(message)
        {
        }
    }
}
