
using System;

namespace muapDotNET.Compiler
{
    [Serializable]
    internal class compileError : Exception
    {
        public compileError()
        {
        }

        public compileError(string message) : base(message)
        {
        }

        public compileError(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}