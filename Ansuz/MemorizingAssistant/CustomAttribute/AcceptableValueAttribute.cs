using System;
using System.Linq;
using System.Text;

namespace Ansuz.MemorizingAssistant.CustomAttribute
{
    /// <summary>
    /// Store acceptable values of fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    internal class AcceptableValueAttribute : Attribute
    {
        /// <summary>
        /// The acceptable values of the field.
        /// </summary>
        public object[] AcceptableValues { get; private set; }

        /// <summary>
        /// The constractor of this attribute.
        /// </summary>
        /// <param name="values">The acceptable values of the field.</param>
        public AcceptableValueAttribute(params object[] values)
        {
            AcceptableValues = values;
        }

        /// <summary>
        /// The overrided ToString method.
        /// </summary>
        /// <returns>The acceptable values of the field.</returns>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder($"Acceptable Values :");
            int charCount = builder.Length;
            // append string form of each object to builder
            Array.ForEach(AcceptableValues, (obj) => 
            {
                charCount += obj.ToString().Length + MemorizingAssistant.AttributeToStringSeparator.Length;
                if (charCount > MemorizingAssistant.WindowWidth - MemorizingAssistant.EmptySpace - MemorizingAssistant.AttributeToStringSeparator.Length)
                {
                    charCount = 0;
                    builder.Append($"{Environment.NewLine}{obj}{MemorizingAssistant.AttributeToStringSeparator}");
                    return;
                }
                builder.Append($"{obj}{MemorizingAssistant.AttributeToStringSeparator}");
            });
            // remove last separator then append period
            builder.Remove(builder.Length - MemorizingAssistant.AttributeToStringSeparator.Length - 1, MemorizingAssistant.AttributeToStringSeparator.Length);
            builder.Append('.');
            return builder.ToString();
        }
    }
}
