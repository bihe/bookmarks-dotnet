using System;

namespace Api.Infrastructure.Security.Exceptions
{
    /// <summary>
    /// specific exception used to indicate that the user lacks authorization
    /// </summary>
    [Serializable]
    public class AuthorizationException : Exception
    {
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="message"></param>
        public AuthorizationException(string message)
            : base(message)
        { }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public AuthorizationException(string message, Exception inner) : base(message, inner)
        { }
    }
}
