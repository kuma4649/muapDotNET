
using System;

namespace muapDotNET.Compiler
{
    [Serializable]
    internal class MusRecov8Exception : Exception
    {
        public MusRecov8Exception()
        {
        }

        public MusRecov8Exception(string message) : base(message)
        {
        }

        public MusRecov8Exception(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}