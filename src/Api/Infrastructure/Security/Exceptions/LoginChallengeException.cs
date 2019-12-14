using System;

namespace Api.Infrastructure.Security.Exceptions
{
    /// <summary>
    /// specific exception used to indicate that a login handling is necessary
    /// </summary>
    [Serializable]
    public class LoginChallengeException : Exception
    {
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="message"></param>
        public LoginChallengeException(string message)
            : base(message)
        { }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public LoginChallengeException(string message, Exception inner) : base(message, inner)
        { }
    }
}
