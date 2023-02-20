using System;

namespace Inedo.TFS
{
    internal sealed class ConsoleException : Exception
    {
        public ConsoleException(string message) : base(message)
        {
        }
    }
}
