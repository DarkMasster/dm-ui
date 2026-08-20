using System;

namespace DM.UI
{
    public class UISystemException : Exception
    {
        public UISystemException(string message) : base(message)
        {
        }

        public UISystemException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}