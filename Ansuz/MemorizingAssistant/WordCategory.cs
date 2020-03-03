using Ansuz.MemorizingAssistant.Setting;

namespace Ansuz.MemorizingAssistant
{
    /// <summary>
    /// The categories of words.
    /// </summary>
    internal enum WordCategory
    {
        /// <summary>
        /// The words that haven't been learnt <see cref="ThesaurusSetting.LearnTimePerWord"/> times.
        /// </summary>
        Unlearned,

        /// <summary>
        /// <para>The words that have been learnt <see cref="ThesaurusSetting.LearnTimePerWord"/> times,</para>
        /// <para>but haven't been revised <see cref="ThesaurusSetting.ReviseTimePerWord"/> times.</para>
        /// </summary>
        Unfamiliar,

        /// <summary>
        /// <para>The words that have been learnt <see cref="ThesaurusSetting.LearnTimePerWord"/> times,</para>
        /// <para>then been revised <see cref="ThesaurusSetting.ReviseTimePerWord"/> times.</para>
        /// </summary>
        Familiar
    }
}
