using Ansuz.MemorizingAssistant.CustomAttribute;

#pragma warning disable CS0649
namespace Ansuz.MemorizingAssistant.Setting
{
    /// <summary>
    /// Settings that apply to current thesaurus.
    /// </summary>
    /// <remarks>
    /// <para>All fields should have <see cref="CommentAttribute"/> and <see cref="DefaultValueAttribute"/>,</para>
    /// <para>all <see cref="int"/> fields should have <see cref="ValueRangeAttribute"/>.</para>
    /// <para>non-<see cref="int"/> fields should have <see cref="AcceptableValueAttribute"/>,</para>
    /// <para>unless its acceptable values are not constant, if so,</para>
    /// <para>its handle method should be place in <see cref="MemorizingAssistant.EditSettings"/> like <see cref="GenericSetting.CurrentThesaurus"/>.</para>
    /// </remarks>
    internal static class ThesaurusSetting
    {
        /// <summary>
        /// When a word has been learnt that much time, it will become a learned word.
        /// </summary>
        [Comment("When a word have been learnt that much time, it will become a learned word.")]
        [ValueRange(10, 2)]
        [DefaultValue(4)]
        public static int LearnTimePerWord;

        /// <summary>
        /// When a word has been revised that much time, it will become a familiar word.
        /// </summary>
        [Comment("When a word have been revised that much time, it will become a familiar word.")]
        [ValueRange(10, 2)]
        [DefaultValue(4)]
        public static int ReviseTimePerWord;

        /// <summary>
        /// Total number of words in each time's learning / revising and group size in learning mode are determined by this value.
        /// </summary>
        [Comment("Total number of words in each time's learning / revising and group size in learning mode are determined by this value.")]
        [ValueRange(20, 5)]
        [DefaultValue(7)]
        public static int LearningGroupSize;
    }
}
