using System;
using System.Runtime.Serialization;

namespace RSSViewer.Utils
{
    public class MagnetLinkFormatException : Exception
    {
        public MagnetLinkFormatException()
        {
        }

        public MagnetLinkFormatException(string message) : base(message)
        {
        }

        public MagnetLinkFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MagnetLinkFormatException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
