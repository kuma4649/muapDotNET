
using System;

namespace muapDotNET.Compiler
{
    [Serializable]
    internal class MusCompileEndException : Exception
    {
        public MusCompileEndException()
        {
        }

        public MusCompileEndException(string message) : base(message)
        {
        }

        public MusCompileEndException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}