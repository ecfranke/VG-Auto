using System;

namespace VgAuto.Core.Domain
{
    public class UserException : Exception
    {
        public UserException(string message) : base(message)
        {
        }
    }

}
