using System;

namespace Ansuz.MemorizingAssistant.CustomAttribute
{
    /// <summary>
    /// Store maximum acceptable value and minimum acceptable value of fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    internal class ValueRangeAttribute : Attribute
    {
        /// <summary>
        /// The maximum acceptable value of the field.
        /// </summary>
        public int MaxValue { get; private set; }

        /// <summary>
        /// The minimum acceptable value of the field.
        /// </summary>
        public int MinValue { get; private set; }

        /// <summary>
        /// The constructor of this attribute.
        /// </summary>
        /// <param name="maxValue">The maximum acceptable value of the field.</param>
        /// <param name="minValue">The minimum acceptable value of the field.</param>
        public ValueRangeAttribute(int maxValue, int minValue)
        {
            MaxValue = maxValue;
            MinValue = minValue;
        }

        /// <summary>
        /// The overrided ToString method.
        /// </summary>
        /// <returns>the maximum acceptable value and minimum acceptable value of fields</returns>
        public override string ToString()
        {
            return $"Maximum Acceptable Value = {MaxValue}{MemorizingAssistant.AttributeToStringSeparator}Minimum Acceptable Value = {MinValue}.";
        }
    }
}
