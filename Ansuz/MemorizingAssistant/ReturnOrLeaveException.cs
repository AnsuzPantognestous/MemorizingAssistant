using System;

namespace Ansuz.MemorizingAssistant
{
    /// <summary>
    /// The exception that use to return to home page or leave program.
    /// </summary>
    public class ReturnOrLeaveException : Exception
    {
        /// <summary>
        /// Is leaving the program.
        /// </summary>
        public readonly bool IsLeave;

        /// <summary>
        /// The Constructor of this class.
        /// </summary>
        /// <param name="isLeave"></param>
        public ReturnOrLeaveException(bool isLeave)
        {
            IsLeave = isLeave;
        }
    }
}
