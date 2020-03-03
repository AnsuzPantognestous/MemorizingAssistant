using System;

namespace Ansuz.MemorizingAssistant.CustomAttribute
{
    /// <summary>
    /// Store default value of fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    internal class DefaultValueAttribute : Attribute
    {
        /// <summary>
        /// The default value of the field.
        /// </summary>
        public object DefaultValue { get; private set; }

        /// <summary>
        /// The constractor of this attribute.
        /// </summary>
        /// <param name="value">The default value of the field.</param>
        public DefaultValueAttribute(object value)
        {
            DefaultValue = value;
        }

        /// <summary>
        /// The overrided ToString method.
        /// </summary>
        /// <returns>The default value of the field.</returns>
        public override string ToString()
        {
            return $"Default Value = {DefaultValue}.";
        }
    }
}
