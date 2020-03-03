using System;

namespace Ansuz.MemorizingAssistant.CustomAttribute
{
    /// <summary>
    /// Store comment of fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    internal class CommentAttribute : Attribute
    {
        /// <summary>
        /// The comment of the field.
        /// </summary>
        public string Comment { get; private set; }

        /// <summary>
        /// The constractor of this attribute.
        /// </summary>
        /// <param name="commment">The comment of the field, it'd better end with period.</param>
        public CommentAttribute(string commment)
        {
            Comment = commment;
        }
        /// <summary>
        /// The overrided ToString method.
        /// </summary>
        /// <returns>The comment of the field.</returns>
        public override string ToString()
        {
            return Comment;
        }
    }
}
