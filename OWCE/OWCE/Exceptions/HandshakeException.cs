using System;
namespace OWCE.Exceptions
{
    public class HandshakeException : Exception
    {
        public bool ShouldDisconnect { get; private set; }

        public HandshakeException(string message, bool shouldDisconnect) : base(message)
        {
            ShouldDisconnect = shouldDisconnect;
        }

    }
}
