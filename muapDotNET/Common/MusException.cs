using System;
using System.Runtime.Serialization;

namespace muapDotNET.Common
{
    [Serializable]
    public class MusException : Exception
    {
        public MusException()
        {
        }
        public MusException(string message) : base(message)
        {
        }

        public MusException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MusException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public MusException(string message, int row, int col) : base(string.Format(msg.get("E0300"), row, col, message))
        {
        }

    }
}
