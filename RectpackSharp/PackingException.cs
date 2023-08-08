using System;

namespace RectpackSharp
{
    public class PackingException : Exception
    {
        public PackingException() : base() { }

        public PackingException(string message) : base(message) { }

        public PackingException(string message,  Exception innerException) : base(message, innerException) { }
    }
}
