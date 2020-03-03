using System;
using Ansuz.MemorizingAssistant.CustomAttribute;

#pragma warning disable CS0649
namespace Ansuz.MemorizingAssistant.Setting
{
    /// <summary>
    /// Settings that apply to all thesaurus.
    /// </summary>
    /// <remarks>
    /// <para>All fields should have <see cref="CommentAttribute"/> and <see cref="DefaultValueAttribute"/>,</para>
    /// <para>all <see cref="int"/> fields should have <see cref="ValueRangeAttribute"/>.</para>
    /// <para>non-<see cref="int"/> fields should have <see cref="AcceptableValueAttribute"/>,</para>
    /// <para>unless its acceptable values are not constant, if so,</para>
    /// <para>its handle method should be place in <see cref="MemorizingAssistant.EditSettings"/> like <see cref="CurrentThesaurus"/>.</para>
    /// </remarks>
    internal static class GenericSetting
    {
        /// <summary>
        /// The name of current thesaurus folder.
        /// </summary>
        [Comment("The name of current thesaurus folder.")]
        [DefaultValue("DefaultThesaurus")]
        public static string CurrentThesaurus;

        /// <summary>
        /// Window's size of program.
        /// </summary>
        [Comment("Window's size of program.")]
        [AcceptableValue(WindowSize.Small, WindowSize.Medium, WindowSize.Large)]
        [DefaultValue(WindowSize.Medium)]
        public static WindowSize WindowSize;

        /// <summary>
        /// Choices per page when choosing options in the program.
        /// </summary>
        [Comment("Choices per page when choosing options in the program.")]
        [ValueRange(14, 7)]
        [DefaultValue(10)]
        public static int ChoicesPerPage;

        /// <summary>
        /// The background color of program.
        /// </summary>
        [Comment("The background color of program.")]
        [AcceptableValue(ConsoleColor.Black, ConsoleColor.Blue, ConsoleColor.Cyan, ConsoleColor.DarkBlue, ConsoleColor.DarkCyan, ConsoleColor.DarkGray, ConsoleColor.DarkGreen, ConsoleColor.DarkMagenta, ConsoleColor.DarkRed, ConsoleColor.DarkYellow, ConsoleColor.Gray, ConsoleColor.Green, ConsoleColor.Magenta, ConsoleColor.Red, ConsoleColor.White, ConsoleColor.Yellow)]
        [DefaultValue(ConsoleColor.Black)]
        public static ConsoleColor BackGroundColor;

        /// <summary>
        /// The font color of program.
        /// </summary>
        [Comment("The font color of program.")]
        [AcceptableValue(ConsoleColor.Black, ConsoleColor.Blue, ConsoleColor.Cyan, ConsoleColor.DarkBlue, ConsoleColor.DarkCyan, ConsoleColor.DarkGray, ConsoleColor.DarkGreen, ConsoleColor.DarkMagenta, ConsoleColor.DarkRed, ConsoleColor.DarkYellow, ConsoleColor.Gray, ConsoleColor.Green, ConsoleColor.Magenta, ConsoleColor.Red, ConsoleColor.White, ConsoleColor.Yellow)]
        [DefaultValue(ConsoleColor.White)]
        public static ConsoleColor FontColor;
    }
}
