using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ansuz.MemorizingAssistant.CustomAttribute;
using Ansuz.MemorizingAssistant.Setting;

namespace Ansuz.MemorizingAssistant
{

    /// <summary>
    /// The main class of this program, contains most of the codes.
    /// </summary>
    public static class MemorizingAssistant
    {
        /// <summary>
        /// <para>The list of all words and their definitions.</para>
        /// <para><see cref="KeyValuePair{TKey, TValue}.Key"/> is words and <see cref="KeyValuePair{TKey, TValue}.Value"/> is definitions.</para>
        /// </summary>
        private static List<KeyValuePair<string, string>> Thesaurus;

        /// <summary>
        /// <para>The list of all words' learning counts and revising counts.</para>
        /// <para><see cref="Tuple{T1, T2}.Item1"/> is learning counts, <see cref="Tuple{T1, T2}.Item2"/> is revising counts.</para>
        /// </summary>
        private static Tuple<List<int>, List<int>> WordsLearningAndRevisingCounts = Tuple.Create(new List<int>(), new List<int>());

        /// <summary>
        /// The list of all the unlearned words' indexes.
        /// </summary>
        private static List<int> UnlearnedWordIndexes = new List<int>();

        /// <summary>
        /// The list of all the unfamiliar words' indexes.
        /// </summary>
        private static List<int> FamiliarWordIndexes = new List<int>();

        /// <summary>
        /// The list of all the familiar words' indexes.
        /// </summary>
        private static List<int> UnfamiliarWordIndexes = new List<int>();


        /// <summary>
        /// The random number generator of this program.
        /// </summary>
        private static Random Rand = new Random();


        /// <summary>
        /// The formatter of this program.
        /// </summary>
        private static BinaryFormatter Formatter = new BinaryFormatter();


        /// <summary>
        /// The variable that is used for storing user's input, should be used by <see cref="Input"/>.
        /// </summary>
        private static string _Input;
        
        /// <summary>
        /// Encapsulated <see cref="_Input"/>, will throw <see cref="ReturnOrLeaveException"/>.
        /// </summary>
        private static string Input
        {
            get
            {
                return _Input;
            }
            set
            {
                _Input = value;

                if (_Input?.ToLower() == "home" || _Input?.ToLower() == "exit")
                {
                    throw new ReturnOrLeaveException(char.ToLowerInvariant(_Input?.First() ?? ' ') == 'e');
                }
            }
        }

        /// <summary>
        /// The category that is learning / revising now.
        /// </summary>
        private static WordCategory CurrentCategory;
        
        /// <summary>
        /// The arrow pattern in choosing page, its length should not greater than <see cref="EmptySpace"/>.
        /// </summary>
        private const string Arrow = "  ==> ";

        /// <summary>
        /// if setting texts' first char is this, it will not be check, this char will be used in <see cref="SaveSettingFile"/>.
        /// </summary>
        private const char SettingCommentChar = '#';

        /// <summary>
        /// The name of this program.
        /// </summary>
        private const string ProgramName = "MemorizingAssistant";

        /// <summary>
        /// <see cref="Console.BufferHeight"/> will be set to this value.
        /// </summary>
        private const int BufferHeight = 80;

        /// <summary>
        /// The empty place width at end of each line.
        /// </summary>
        internal const int EmptySpace = 20;

        /// <summary>
        /// The separator in attributes' string format.
        /// </summary>
        internal const string AttributeToStringSeparator = ", ";

        /// <summary>
        /// Get only, return a array that contains names of folders that have thesaurus file in the same directory with this program.
        /// </summary>
        private static string[] ThesaurusList
        {
            get
            {
                return Array.FindAll(Array.ConvertAll(Directory.GetDirectories(Directory.GetCurrentDirectory()), (str) => { return str.Remove(0, Directory.GetCurrentDirectory().Length + 1); }), (str) => { return File.Exists($"{str}/Thesaurus.dat"); });
            }
        }

        /// <summary>
        /// Get only, return fitting window width by <see cref="GenericSetting.WindowSize"/>.
        /// </summary>
        internal static int WindowWidth
        {
            get
            {
                switch (GenericSetting.WindowSize)
                {
                    case WindowSize.Small:
                        return 120;
                    case WindowSize.Medium:
                        return 160;
                    case WindowSize.Large:
                        return 200;
                    default:
                        return 160;
                }
            }
        }

        /// <summary>
        /// Get only, return fitting window height by <see cref="GenericSetting.WindowSize"/>.
        /// </summary>
        internal static int WindowHeight
        {
            get
            {
                switch (GenericSetting.WindowSize)
                {
                    case WindowSize.Small:
                        return 30;
                    case WindowSize.Medium:
                        return 40;
                    case WindowSize.Large:
                        return 50;
                    default:
                        return 40;
                }
            }
        }

        /// <summary>
        /// The main method of this program, will be called automatically when this program is started.
        /// </summary>
        /// <remarks>
        /// <para>Firstly, this method will initialize the program and read generic setting file,</para>
        /// <para>if there's no any thesaurus or current thesaurus doesn't exit,</para>
        /// <para>it will let user create or choose thesaurus.</para>
        /// <para>Next, it will read thesaurus setting file and learning record,</para>
        /// <para>Then enter the home page of this program.</para>
        /// </remarks>
        public static void Main()
        {
            Console.Title = ProgramName;

            ReadSettingFile(true);

            // set window size and buffer size until user leaves the program
            Task setWindowSize = Task.Run(action: () =>
            {
                while (true)
                {
                    Console.SetWindowSize(WindowWidth, WindowHeight);
                    Console.SetBufferSize(WindowWidth, BufferHeight);
                    Thread.Sleep(100);
                }
            });

            SetColor();

            while (ThesaurusList.Length == 0)
            {
                Console.Clear();
                Console.WriteLine("There's no any thesaurus, press any key to create thesaurus.");
                Console.ReadKey(true);
                CreateThesaurus();
            }

            if (!ThesaurusList.Contains(GenericSetting.CurrentThesaurus))
            {
                Console.WriteLine("Current thesaurus does not exist, press any key to choose thesaurus.");
                Console.ReadKey(true);
                ChoosingPage(ThesaurusList, "Thesauruses", false);
                GenericSetting.CurrentThesaurus = Input;
            }

            ThesaurusInit();

            HomePage();
        }

        /// <summary>
        /// Read settings from setting file.
        /// </summary>
        /// <param name="isGenericSetting">
        /// <para>If true, this method will read generic settings,</para>
        /// <para>otherwise it will read thesaurus settings from current thesaurus.</para>
        /// </param>
        private static void ReadSettingFile(bool isGenericSetting)
        {
            // get fields of current settings, then set them to default value
            FieldInfo[] fields = (isGenericSetting ? typeof(GenericSetting) : typeof(ThesaurusSetting)).GetFields(BindingFlags.Public | BindingFlags.Static);
            Array.ForEach(fields, (field) => { field.SetValue(null, field.GetCustomAttribute<DefaultValueAttribute>().DefaultValue); });

            // use linked list not array or array list is because fields will be remove from list if they have been matched with setting texts to avoid redundant check
            LinkedList<FieldInfo> fieldList = new LinkedList<FieldInfo>(fields);
            
            string path = $"{(isGenericSetting ? "Generic" : $"{GenericSetting.CurrentThesaurus}/")}Setting.txt";
            FileStream SettingFile = new FileStream(path, FileMode.OpenOrCreate);

            if (SettingFile.Length == 0)
            {
                SettingFile.Dispose();
                SaveSettingFile(isGenericSetting, fields);
                Console.WriteLine($"There's no {(isGenericSetting ? "generic setting file" : "setting file in this thesaurus")}, new setting file is generated, press any key to continue.");
                Console.ReadKey(true);
                Console.Clear();
                return;
            }
            
            SettingFile.Dispose();
            StreamReader reader = new StreamReader(path);
            LinkedList<string> SettingTexts = new LinkedList<string>();
            while ((Input = reader.ReadLine()) != null)
            {
                if (Input.First() != SettingCommentChar)
                {
                    //current text is not comment so add it
                    SettingTexts.AddLast(Input);
                }
            }
            reader.Dispose();

            // those three bool should be assigned to true if the situation they represent has emerged
            bool isMissing = false;
            bool isParsingError = false;
            bool notAcceptable = false;
            
            LinkedListNode<string> currentTextNode = SettingTexts.First;
            LinkedListNode<FieldInfo> currentFieldNode = fieldList.First;
            LinkedListNode<FieldInfo> startFieldNode = fieldList.First;

            // first member should be field name and second member should be the value
            string[] texts = Array.ConvertAll(currentTextNode.Value.Split('='), (str) => { return str.Trim(); });

            // remove current text node from list then assigned variable currentTextNode to the next node, will also update texts 
            void RemoveCurrentTextNodeAndMoveToNext()
            {
                currentTextNode = currentTextNode.Next ?? SettingTexts.First;
                SettingTexts.Remove(currentTextNode.Previous ?? SettingTexts.Last);

                texts = Array.ConvertAll(currentTextNode.Value.Split('='), (str) => { return str.Trim(); });
            }

            // remove current field node from list if it is needed, then assigned currentFieldNode to the next node
            void MoveToNextFieldNode(bool deletePrevious)
            {
                currentFieldNode = currentFieldNode.Next ?? fieldList.First;

                if (deletePrevious)
                {
                    fieldList.Remove(currentFieldNode.Previous ?? fieldList.Last);
                }
            }

            void SetFieldValue()
            {
                if (currentFieldNode.Value.FieldType == typeof(int))
                {
                    if (int.TryParse(texts[1], out int temp))
                    {
                        ValueRangeAttribute range = currentFieldNode.Value.GetCustomAttribute<ValueRangeAttribute>();

                        //Constrain value in range if range attribute exist, otherwise value will be simply assigned
                        currentFieldNode.Value.SetValue(null, Math.Max(Math.Min(temp, range?.MaxValue ?? temp), range?.MinValue ?? temp));
                        if (temp > range?.MaxValue || temp < range?.MinValue)
                        {
                            Console.WriteLine($"Setting {currentFieldNode.Value.Name}'s value are too {(temp > range.MaxValue ? "big" : "small")}, the value is set to {(temp > range.MaxValue ? "max" : "min")} value \"{Math.Max(Math.Min(temp, range.MaxValue), range.MinValue)}\", press any key to continue reading setting file.");
                            Console.ReadKey(true);
                            Console.Clear();
                        }
                    }
                    else
                    {
                        isParsingError = true;
                    }
                }

                if (currentFieldNode.Value.FieldType == typeof(string))
                {
                    AcceptableValueAttribute values = currentFieldNode.Value.GetCustomAttribute<AcceptableValueAttribute>();
                    if (values?.AcceptableValues.Contains(texts[1]) ?? false)
                    {
                        // the value in setting file is acceptable
                        currentFieldNode.Value.SetValue(null, texts[1]);
                    }
                    else
                    {
                        if (values == null)
                        {
                            // there's no acceptable value attribute
                            currentFieldNode.Value.SetValue(null, texts[1]);
                        }
                        else
                        {
                            currentFieldNode.Value.SetValue(null, currentFieldNode.Value.GetCustomAttribute<DefaultValueAttribute>().DefaultValue);
                            notAcceptable = true;
                        }
                    }
                }

                if (currentFieldNode.Value.FieldType.IsEnum)
                {
                    int index = Array.IndexOf(currentFieldNode.Value.FieldType.GetEnumNames(), texts[1]);
                    if (index != -1)
                    {
                        // the value in setting file is acceptable
                        currentFieldNode.Value.SetValue(null, ((int[])currentFieldNode.Value.FieldType.GetEnumValues())[index]);
                    }
                    else
                    {
                        currentFieldNode.Value.SetValue(null, currentFieldNode.Value.GetCustomAttribute<DefaultValueAttribute>().DefaultValue);
                        notAcceptable = true;
                    }
                }
            }

            while (fieldList.Count > 0 && SettingTexts.Count > 0)
            {
                if (texts.Length < 2)
                {
                    // this entry is not well-formatted
                    isParsingError = true;
                    RemoveCurrentTextNodeAndMoveToNext();
                }

                if (texts[0] == currentFieldNode.Value.Name)
                {
                    // current text is matched with current field, set the field's value to the value in setting text,
                    // then delete two nodes from their list and assign two variables to next nodes,
                    //at last, set the startFieldNode to currentFieldNode to record the start of this round
                    SetFieldValue();
                    RemoveCurrentTextNodeAndMoveToNext();
                    MoveToNextFieldNode(true);
                    startFieldNode = currentFieldNode;
                    continue;
                }

                // text can not be match, assign currentFieldNode to next to check if this node can be matched with current text
                MoveToNextFieldNode(false);
                
                if (currentFieldNode == startFieldNode)
                {
                    // all fields have been check, all of them can not be matched with current text, so remove it then check the next text
                    RemoveCurrentTextNodeAndMoveToNext();
                }
            }

            // all texts have been removed from text list when be matched with a field or can not be matched with any field,
            // if there's still some fields in field list, that means those fields' setting are missing
            isMissing = fieldList.Count > 0;

            SaveSettingFile(isGenericSetting, fields);

            if (isParsingError || isMissing || notAcceptable)
            {
                Console.WriteLine($"In {(isGenericSetting ? "generic" : $"thesaurus {GenericSetting.CurrentThesaurus}'s")} setting:");
                Console.WriteLine($"Some settings can not be {(isParsingError ? "parsed" : (isMissing ? "found" : "accepted"))}, those settings were rewrited to default value, press any key to continue.");
                Console.ReadKey(true);
                Console.Clear();
            }
        }

        /// <summary>
        /// Save the settings into save file.
        /// </summary>
        /// <param name="isGenericSetting">
        /// <para>If true, this method will save generic settings,</para>
        /// <para>otherwise it will save thesaurus settings to current thesaurus.</para>
        /// </param>
        /// <param name="fields">The fields that will be save to file.</param>
        private static void SaveSettingFile(bool isGenericSetting, FieldInfo[] fields)
        {
            StreamWriter writer = new StreamWriter($"{(isGenericSetting ? "Generic" : $"{GenericSetting.CurrentThesaurus}/")}Setting.txt");
            StringBuilder builder = new StringBuilder();
            Array.ForEach(fields, (field) => 
            {
                Array.ForEach(field.GetCustomAttributes().ToArray(), (attr) => 
                {
                    // add a line of comment or description of an attribute of the field
                    builder.Append($"{SettingCommentChar} {attr.ToString().Replace(Environment.NewLine, "")}{Environment.NewLine}");
                });
                // add the name and value and a empty line
                builder.Append($"{field.Name} = {field.GetValue(null)}{Environment.NewLine}{SettingCommentChar}{Environment.NewLine}");
            });
            // remove the last empty line
            builder.Remove(builder.Length - (Environment.NewLine.Length * 2 + 1), Environment.NewLine.Length * 2 + 1);
            writer.Write(builder.ToString());
            writer.Dispose();
        }

        /// <summary>
        /// <para>Show a page that user can choose options by arrow key and confirm by enter,</para>
        /// <para>and the chosen option will be assigned to <see cref="Input"/>.</para>
        /// </summary>
        /// <param name="choices">Options to be selected.</param>
        /// <param name="choiceGeneralName">The general name of options, the first letter should in uppercase.</param>
        /// <param name="showHomeAndExit">Should this method show home option and exit option.</param>
        /// <param name="choicesComment">Optional, the comment of all options, will be print under corresponding option.</param>
        /// <param name="additionalInfo">this method is optional, should print additional information.</param>
        /// <param name="backgroundColors">The background colors of each option and comments.</param>
        /// <param name="fontColors">The font colors of each option and comments.</param>
        /// <returns>The index of the chosen option, -1 means home, -2 means exit.</returns>
        private static int ChoosingPage(string[] choices, string choiceGeneralName, bool showHomeAndExit, string[] choicesComment = null, Action additionalInfo = null, ConsoleColor[] backgroundColors = null, ConsoleColor[] fontColors = null)
        {
            int currentPage = 0;
            int currentChoiceInThisPage = 0;
            int CursorHeightAtEnd;
            int previousChoiceCount = currentPage * GenericSetting.ChoicesPerPage;
            int choiceCountInCurrentPage = Math.Min(choices.Length - previousChoiceCount, GenericSetting.ChoicesPerPage);
            int totalPageCount = (int)Math.Ceiling(((double)choices.Length) / GenericSetting.ChoicesPerPage);

            // record the height of each choice, the last value is the height of the end of last choice
            int[] CursorHeights = new int[GenericSetting.ChoicesPerPage + (showHomeAndExit ? 2 : 0) + 1];

            void PrintArrow()
            {
                Console.Write(Arrow);
                Console.CursorLeft = 0;
            }

            void PrintChoice()
            {
                Console.Clear();

                // print additional information
                additionalInfo?.Invoke();

                // print the general name of choices and the page count if there's more than 1 page
                Console.WriteLine($"{choiceGeneralName}:{(totalPageCount > 1 ? $"\t\tpage: {currentPage + 1} / {totalPageCount}" : "")}{Environment.NewLine}");
                // update data
                choiceCountInCurrentPage = Math.Min(choices.Length - previousChoiceCount, GenericSetting.ChoicesPerPage);
                
                for (int i = 0; i < choiceCountInCurrentPage; i++)
                {
                    CursorHeights[i] = Console.CursorTop;

                    // array null check and index range check
                    if (backgroundColors != null && backgroundColors.Length > previousChoiceCount + i)
                    {
                        Console.BackgroundColor = backgroundColors[previousChoiceCount + i];
                    }

                    // array null check and index range check
                    if (fontColors != null && fontColors.Length > previousChoiceCount + i)
                    {
                        Console.ForegroundColor = fontColors[previousChoiceCount + i];
                    }

                    Console.WriteLine(choices[previousChoiceCount + i]);

                    // array null check, index range check and element null check
                    if (choicesComment != null && choicesComment.Length > previousChoiceCount + i && choicesComment[previousChoiceCount + i] != null)
                    {
                        Console.WriteLine(choicesComment[previousChoiceCount + i]);
                    }
                    Console.WriteLine();
                }

                Console.BackgroundColor = GenericSetting.BackGroundColor;
                Console.ForegroundColor = GenericSetting.FontColor;

                if (showHomeAndExit)
                {
                    // print home option and exit option
                    Console.WriteLine();
                    CursorHeights[choiceCountInCurrentPage] = Console.CursorTop;
                    Console.WriteLine($"Home{Environment.NewLine}Return to home page.{Environment.NewLine}");
                    CursorHeights[choiceCountInCurrentPage + 1] = Console.CursorTop;
                    Console.WriteLine($"Exit{Environment.NewLine}Leave this program.{Environment.NewLine}");
                }

                // update the height of the end of last choice, then move the chosen choice a little bit and print a arrow to emphasize it 
                CursorHeights[choiceCountInCurrentPage + (showHomeAndExit ? 2 : 0)] = Console.CursorTop - 1;
                Console.MoveBufferArea(0, CursorHeights[currentChoiceInThisPage], WindowWidth - EmptySpace, CursorHeights[currentChoiceInThisPage + 1] - CursorHeights[currentChoiceInThisPage], Arrow.Length, CursorHeights[currentChoiceInThisPage]);
                Console.WriteLine($"Press up and down to choose {choiceGeneralName.ToLower()}, {(totalPageCount > 1 ? "press left and right to turn pages, " : "")}press enter to confirm your choice.{Environment.NewLine}");
                CursorHeightAtEnd = Console.CursorTop;
                Console.CursorTop = CursorHeights[currentChoiceInThisPage];
                PrintArrow();
            }

            Console.CursorVisible = false;

            PrintChoice();
            ConsoleKey key;
            while ((key = Console.ReadKey(true).Key) != ConsoleKey.Enter)
            {
                if (key == ConsoleKey.UpArrow)
                {
                    // change the chosen choice
                    Console.MoveBufferArea(Arrow.Length, CursorHeights[currentChoiceInThisPage], WindowWidth - EmptySpace, CursorHeights[currentChoiceInThisPage + 1] - CursorHeights[currentChoiceInThisPage], 0, CursorHeights[currentChoiceInThisPage]);
                    currentChoiceInThisPage = (currentChoiceInThisPage - 1 + choiceCountInCurrentPage + (showHomeAndExit ? 2 : 0)) % (choiceCountInCurrentPage + (showHomeAndExit ? 2 : 0));
                    Console.MoveBufferArea(0, CursorHeights[currentChoiceInThisPage], WindowWidth - EmptySpace, CursorHeights[currentChoiceInThisPage + 1] - CursorHeights[currentChoiceInThisPage], Arrow.Length, CursorHeights[currentChoiceInThisPage]);
                    Console.CursorTop = CursorHeights[currentChoiceInThisPage];
                    PrintArrow();
                }

                if (key == ConsoleKey.DownArrow)
                {
                    // change the chosen choice
                    Console.MoveBufferArea(Arrow.Length, CursorHeights[currentChoiceInThisPage], WindowWidth - EmptySpace, CursorHeights[currentChoiceInThisPage + 1] - CursorHeights[currentChoiceInThisPage], 0, CursorHeights[currentChoiceInThisPage]);
                    currentChoiceInThisPage = (currentChoiceInThisPage + 1) % (choiceCountInCurrentPage + (showHomeAndExit ? 2 : 0));
                    Console.MoveBufferArea(0, CursorHeights[currentChoiceInThisPage], WindowWidth - EmptySpace, CursorHeights[currentChoiceInThisPage + 1] - CursorHeights[currentChoiceInThisPage], Arrow.Length, CursorHeights[currentChoiceInThisPage]);
                    Console.CursorTop = CursorHeights[currentChoiceInThisPage];
                    PrintArrow();
                }

                if (key == ConsoleKey.LeftArrow && totalPageCount > 1)
                {
                    // turn the page and constrain the currentChoice to avoid index out of range
                    currentPage = (currentPage + totalPageCount - 1) % totalPageCount;
                    previousChoiceCount = currentPage * GenericSetting.ChoicesPerPage;
                    currentChoiceInThisPage = Math.Min(currentChoiceInThisPage, choices.Length - previousChoiceCount + (showHomeAndExit ? 2 : 0) - 1);
                    PrintChoice();
                }

                if (key == ConsoleKey.RightArrow && totalPageCount > 1)
                {
                    // turn the page and constrain the currentChoice to avoid index out of range
                    currentPage = (currentPage + 1) % totalPageCount;
                    previousChoiceCount = currentPage * GenericSetting.ChoicesPerPage;
                    currentChoiceInThisPage = Math.Min(currentChoiceInThisPage, choices.Length - previousChoiceCount + (showHomeAndExit ? 2 : 0) - 1);
                    PrintChoice();
                }

                if (CursorHeightAtEnd > Console.WindowHeight)
                {
                    if (Console.CursorTop <= Console.WindowHeight)
                    {
                        // move the window to show the information that at top of the page
                        Console.WindowTop = 0;
                    }
                    else
                    {
                        if (Console.CursorTop > CursorHeightAtEnd - Console.WindowHeight - 1)
                        {
                            // move the window to show the information that at end of the page
                            Console.WindowTop = CursorHeightAtEnd - Console.WindowHeight - 1;
                        }
                    }
                }
            }
            Console.CursorTop = CursorHeightAtEnd;
            Console.CursorLeft = 0;
            Console.CursorVisible = true;

            if (currentChoiceInThisPage == choiceCountInCurrentPage)
            {
                Input = "Home";
                return -1;
            }

            if (currentChoiceInThisPage == choiceCountInCurrentPage + 1)
            {
                Input = "Exit";
                return -2;
            }

            Input = choices[previousChoiceCount + currentChoiceInThisPage];
            return previousChoiceCount + currentChoiceInThisPage;
        }
        
        /// <summary>
        /// Set background color and font color to colors in generic setting.
        /// </summary>
        private static void SetColor()
        {
            Console.BackgroundColor = GenericSetting.BackGroundColor;
            Console.ForegroundColor = GenericSetting.FontColor;
            string str = new string(' ', Console.BufferWidth);
            Console.SetCursorPosition(0, 0);
            for (int i = 0; i < Console.BufferHeight; i++)
            {
                // print empty chars to change the background color of program
                Console.Write(str);
            }
            Console.SetCursorPosition(0, 0);
        }

        /// <summary>
        /// <para>Initialize <see cref="Thesaurus"/> by encipher the thesaurus file of current thesaurus.</para>
        /// <para>This method will also call <see cref="ReadSettingFile"/> and <see cref="ReadLearningRecord"/>.</para>
        /// </summary>
        private static void ThesaurusInit()
        {
            FileStream thesaurusFile = new FileStream($"{GenericSetting.CurrentThesaurus}/Thesaurus.dat", FileMode.Open);
            // read the thesaurus file then encipher the strings
            Thesaurus = ((List<KeyValuePair<string, string>>)Formatter.Deserialize(thesaurusFile)).ConvertAll((pair) => 
            {
                return new KeyValuePair<string, string>(Encoding.Default.GetString(Convert.FromBase64String(pair.Key)), Encoding.Default.GetString(Convert.FromBase64String(pair.Value)));
            });
            thesaurusFile.Dispose();
            ReadSettingFile(false);
            ReadLearningRecord();
        }

        /// <summary>
        /// Read the learning record from save file of current thesaurus (see <see cref="GenericSetting.CurrentThesaurus"/>).
        /// </summary>
        private static void ReadLearningRecord()
        {
            // clear the lists to avoid storing multiple thesauruses' learning record at the same time
            WordsLearningAndRevisingCounts.Item1.Clear();
            WordsLearningAndRevisingCounts.Item2.Clear();
            UnlearnedWordIndexes.Clear();
            UnfamiliarWordIndexes.Clear();
            FamiliarWordIndexes.Clear();
            
            FileStream saveFile = new FileStream($"{GenericSetting.CurrentThesaurus}/LearningRecord.dat", FileMode.OpenOrCreate);

            if (saveFile.Length == 0)
            {
                saveFile.Dispose();
                for (int i = 0; i < Thesaurus.Count; i++)
                {
                    WordsLearningAndRevisingCounts.Item1.Add(0);
                    WordsLearningAndRevisingCounts.Item2.Add(0);
                    UnlearnedWordIndexes.Add(i);
                }
                SaveLearningRecord();
                Console.WriteLine("There's no learning record in this thesaurus, new learning record file is generated, press any key to continue.");
                Console.ReadKey(true);
                Console.Clear();
                return;
            }

            // read the learning record from file
            WordsLearningAndRevisingCounts = (Tuple<List<int>, List<int>>)Formatter.Deserialize(saveFile);
            saveFile.Dispose();

            for (int i = 0; i < WordsLearningAndRevisingCounts.Item1.Count; i++)
            {
                if (WordsLearningAndRevisingCounts.Item1[i] < ThesaurusSetting.LearnTimePerWord)
                {
                    // this word hasn't learnt enough
                    UnlearnedWordIndexes.Add(i);
                }
                if (WordsLearningAndRevisingCounts.Item1[i] >= ThesaurusSetting.LearnTimePerWord && WordsLearningAndRevisingCounts.Item2[i] < ThesaurusSetting.ReviseTimePerWord)
                {
                    // this word has learnt enough but hasn't revised enough
                    UnfamiliarWordIndexes.Add(i);
                }
                if (WordsLearningAndRevisingCounts.Item2[i] >= ThesaurusSetting.ReviseTimePerWord)
                {
                    // this word has revised enough
                    FamiliarWordIndexes.Add(i);
                }
            }

            if (WordsLearningAndRevisingCounts.Item1.Count < Thesaurus.Count)
            {
                // some of the learning record is missing
                for (int i = WordsLearningAndRevisingCounts.Item1.Count; i < Thesaurus.Count; i++)
                {
                    WordsLearningAndRevisingCounts.Item1.Add(0);
                    WordsLearningAndRevisingCounts.Item2.Add(0);
                    UnlearnedWordIndexes.Add(i);
                }
                SaveLearningRecord();
            }
        }


        /// <summary>
        /// <para>The home page of the program, input home will return from its called methods.</para>
        /// <para>Unless input exit, the loop in this method will not end.</para>
        /// </summary>
        private static void HomePage()
        {
            string[] homePageOptions = { "Study", "Other", "Exit" };
            string[] homePageOptionComments = {
                "Learning and revising words.",
                "Other options.",
                "Leave this program."
            };

            while (true)
            {
                try
                {
                    ChoosingPage(homePageOptions, "Options", false, homePageOptionComments, () =>
                    {
                        Console.WriteLine($"Home Page{Environment.NewLine}If you want to leave this program when learning or revising, please use exit,{Environment.NewLine}otherwise learning record will not be save.{Environment.NewLine}");
                    });

                    switch (Input)
                    {
                        case "Study":
                            StudyPage();
                            break;
                        case "Other":
                            OtherPage();
                            break;
                    }
                }
                catch (ReturnOrLeaveException e)
                {
                    if (e.IsLeave)
                    {
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// The page that user can choose study modes.
        /// </summary>
        private static void StudyPage()
        {
            string[] studyPageOptions = { "Learning Mode", "Spelling Mode", "Choosing Mode" };
            string[] studyPageOptionComments = {
                    "Learn unlearned words.",
                    "Revise words by see the definitions and try to spell the right words.",
                    "Revise words by choose the right definitions of words."
            };

            ChoosingPage(studyPageOptions, "Study modes", true, studyPageOptionComments);

            switch (Input)
            {
                case "Learning Mode":
                    LearningMode();
                    break;
                case "Spelling Mode":
                    SpellingMode();
                    break;
                case "Choosing Mode":
                    ChoosingMode();
                    break;
            }
        }

        /// <summary>
        /// In this method, unlearned words will be shown to user to be learnt.
        /// </summary>
        /// <remarks>
        /// <para>Firstly, a list of unlearned words will be initialized by method <see cref="WordOrderInit"/>,</para>
        /// <para>then the words will be divided into several groups, group sizes are equal to <see cref="ThesaurusSetting.LearningGroupSize"/>.</para>
        /// <para>Next, words and their definitions will be shown, user has to spell them.</para>
        /// <para>if user inputs home or exit, this method will return to <see cref="HomePage"/>.</para>
        /// <para>If a word has been learnt <see cref="ThesaurusSetting.LearnTimePerWord"/>  times,</para>
        /// <para>user will be asked to add this word to unfamiliar category now or next time.</para>
        /// <para>Words can be skipped, it will appear later.</para>
        /// <para>they can also be moved into familiar words, if so, it will be deleted from this time's learning.</para>
        /// <para>After learning of a group of words, all the words and their definitions in this group will be printed,</para>
        /// <para>and this method will ask user do they want to return to home page.</para>
        /// <para>If user choose to return or learning of all groups is over,</para>
        /// <para>learning record will be saved by <see cref="SaveLearningRecord"/> and this method will return to <see cref="HomePage"/>.</para>
        /// </remarks>
        private static void LearningMode()
        {
            CurrentCategory = WordCategory.Unlearned;
            List<int> order = WordOrderInit();

            if (order.Count == 0)
            {
                return;
            }
            
            int groupSize = Math.Min(order.Count, ThesaurusSetting.LearningGroupSize);
            int groupCount = (int)Math.Ceiling(((double)order.Count) / groupSize);
            int innerIndex;
            int outerIndex;
            int previousWordCount;

            void LearningModeInfo()
            {
                Console.WriteLine("All the words you want to learn / revise will be shown in this mode.");
                Console.WriteLine("Spelling is required for the memorizing.");
                Console.WriteLine("Input skip to skip current word, if you do so, it will appear later.");
                Console.WriteLine("Input familiar to remove current word from learning, and add it into familiar words.");
                GenericInfo();
            }

            void QuestionInfo()
            {
                LearningModeInfo();
                // print current word, no. of current word in current group, current group size and definition of current word
                Console.WriteLine($"{Thesaurus[order[previousWordCount + innerIndex]].Key}\t\t{innerIndex + 1} / {Math.Min(order.Count - previousWordCount, groupSize)}{Environment.NewLine}{Environment.NewLine}{Thesaurus[order[previousWordCount + innerIndex]].Value}{Environment.NewLine}");
            }

            // will be used in method Questioning(), return true means skip current question
            bool SkipCheck()
            {
                if (Input == "skip")
                {
                    // duplicate current question to later on in this time's learning and skip it
                    order.Insert(Rand.Next(previousWordCount + innerIndex, order.Count + 1), order[previousWordCount + innerIndex]);
                    groupSize = Math.Min(order.Count, ThesaurusSetting.LearningGroupSize);
                    groupCount = (int)Math.Ceiling(((double)order.Count) / groupSize);
                    Console.WriteLine("This word will be skipped, it will appear later, press any key to continue.");
                    Console.ReadKey(true);
                    return true;
                }

                if (Input == "familiar")
                {
                    // delete current question and its duplication then make current word familiar, index-- to avoid skip of next question
                    int currentWord = order[previousWordCount + innerIndex];
                    WordsLearningAndRevisingCounts.Item1[currentWord] = ThesaurusSetting.LearnTimePerWord;
                    WordsLearningAndRevisingCounts.Item2[currentWord] = ThesaurusSetting.ReviseTimePerWord;
                    UnlearnedWordIndexes.Remove(currentWord);
                    FamiliarWordIndexes.Add(currentWord);
                    for (int k = previousWordCount + innerIndex; k < order.Count; k++)
                    {
                        if (order[k] == currentWord)
                        {
                            order.RemoveAt(k);
                            k--;
                        }
                    }
                    groupSize = Math.Min(order.Count, ThesaurusSetting.LearningGroupSize);
                    groupCount = (int)Math.Ceiling(((double)order.Count) / groupSize);
                    innerIndex--;
                    Console.WriteLine("This word is a familiar word now, you won't learn it in learn mode anymore, press any key to continue.");
                    Console.ReadKey(true);
                    return true;
                }

                return false;
            }

            for (outerIndex = 0; outerIndex < groupCount; outerIndex++)
            {
                previousWordCount = outerIndex * groupSize;
                for (innerIndex = 0; innerIndex < Math.Min(order.Count - previousWordCount, groupSize); innerIndex++)
                {
                    // call the method Questioning() to ask question
                    Questioning(order, previousWordCount + innerIndex, Thesaurus[order[previousWordCount + innerIndex]].Key, Console.ReadLine, () =>
                    {
                        Console.WriteLine("Wrong spelling, press any key to retry.");
                        Console.ReadKey(true);
                        return false;
                    },
                    QuestionInfo, SkipCheck);
                }
                
                // after learning of a group of words, show those words and their definitions
                Console.Clear();
                LearningModeInfo();

                for (innerIndex = 0; innerIndex < Math.Min(order.Count - previousWordCount, groupSize); innerIndex++)
                {
                    // print all words that in current group and their definitions
                    Console.WriteLine($"{Thesaurus[order[previousWordCount + innerIndex]].Key}{Environment.NewLine}{Environment.NewLine}{Thesaurus[order[previousWordCount + innerIndex]].Value}{Environment.NewLine}");
                }

                if (outerIndex == groupCount - 1)
                {
                    Console.WriteLine("This time's learning is finished, press any key to return to home.");
                    SaveLearningRecord();
                    Console.ReadKey(true);
                    return;
                }

                Console.WriteLine("All words in this group were learned, press h to return to home, ");
                Console.WriteLine("press other key to continue.");
                if (Console.ReadKey(true).KeyChar == 'h')
                {
                    SaveLearningRecord();
                    return;
                }
            }
        }

        /// <summary>
        /// Add <see cref="ThesaurusSetting.LearnTimePerWord"/> or <see cref="ThesaurusSetting.ReviseTimePerWord"/> groups of indexes of words in current category into a list.
        /// </summary>
        /// <remarks>
        /// <para>Group size of word groups will be <see cref="ThesaurusSetting.LearningGroupSize"/>,</para>
        /// <para>first group words had never been learnt / revised, second group words had been learnt / revise 1 time, and so on.</para>
        /// <para>If words that had been learnt / revised 1 time are less than <see cref="ThesaurusSetting.LearningGroupSize"/>,</para>
        /// <para>the difference between two numbers will be fill with some unlearned / unrevised words as substitution,</para>
        /// <para>those words will be learnt / revised twice, this also applicable to other groups,</para>
        /// <para>the difference is that substitution may be learnt / revised more than twice.</para>
        /// </remarks>
        /// <returns>The list that contains the indexes of words that should be learnt / revised.</returns>
        private static List<int> WordOrderInit()
        {
            List<int> order = new List<int>();
            List<List<int>> classifyList;
            int currentBranch;
            int index;
            int WordsCount;

            switch (CurrentCategory)
            {
                case WordCategory.Unlearned:
                    classifyList = new List<List<int>>();

                    for (int branch = 0; branch < ThesaurusSetting.LearnTimePerWord; branch++)
                    {
                        // add indexes of words into current branch if the learn times of words equal to branch no.
                        classifyList.Add(UnlearnedWordIndexes.FindAll((i) => { return WordsLearningAndRevisingCounts.Item1[i] == branch; }));
                    }

                    currentBranch = ThesaurusSetting.LearnTimePerWord - 1;

                    // randomly choose a group of words that have been learnt currentBranch times,
                    // then insert them into order list to make them will have been learnt currentBranch + 1 times,
                    // if current branch doesn't have enough words, the words from lower branch will be chosen and insert them into order list muiltiple times,
                    // so those words will have been learnt currentBranch + 1 times
                    // this will be executed on all branches, unless unlearned words is not enough
                    for (int i = ThesaurusSetting.LearnTimePerWord - 1; i >= 0 ; i--)
                    {
                        currentBranch = Math.Min(currentBranch, i);
                        WordsCount = 0;
                        while (WordsCount < ThesaurusSetting.LearningGroupSize)
                        {
                            if (classifyList[currentBranch].Count > 0)
                            {
                                index = Rand.Next(classifyList[currentBranch].Count);
                                for (int j = 0; j < i - currentBranch + 1; j++)
                                {
                                    order.Insert(Rand.Next(order.Count + 1), classifyList[currentBranch][index]);
                                }
                                classifyList[currentBranch].RemoveAt(index);
                                WordsCount++;
                            }
                            else
                            {
                                currentBranch--;
                                if (currentBranch < 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    break;

                case WordCategory.Unfamiliar:
                    classifyList = new List<List<int>>();

                    for (int branch = 0; branch < ThesaurusSetting.ReviseTimePerWord; branch++)
                    {
                        // add indexes of words into current branch if the revise times of words equal to branch no.
                        classifyList.Add(UnfamiliarWordIndexes.FindAll((i) => { return WordsLearningAndRevisingCounts.Item2[i] == branch; }));
                    }

                    currentBranch = ThesaurusSetting.ReviseTimePerWord - 1;

                    // randomly choose a group of words that have been revised currentBranch times,
                    // then insert them into order list to make them will have been revised currentBranch + 1 times,
                    // if current branch doesn't have enough words, the words from lower branch will be chosen and insert them into order list muiltiple times,
                    // so those words will have been revised currentBranch + 1 times
                    // this will be executed on all branches, unless unfamiliar words is not enough
                    for (int i = ThesaurusSetting.ReviseTimePerWord - 1; i >= 0; i--)
                    {
                        currentBranch = Math.Min(currentBranch, i);
                        WordsCount = 0;
                        while (WordsCount < ThesaurusSetting.LearningGroupSize)
                        {
                            if (classifyList[currentBranch].Count > 0)
                            {
                                index = Rand.Next(classifyList[currentBranch].Count);
                                for (int j = 0; j < i - currentBranch + 1; j++)
                                {
                                    order.Insert(Rand.Next(order.Count + 1), classifyList[currentBranch][index]);
                                }
                                classifyList[currentBranch].RemoveAt(index);
                                WordsCount++;
                            }
                            else
                            {
                                currentBranch--;
                                if (currentBranch < 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    break;

                case WordCategory.Familiar:
                    // all familiar words will be simply add into order list
                    FamiliarWordIndexes.ForEach((i) => { order.Insert(Rand.Next(order.Count + 1), i); });
                    break;
            }

            if (order.Count == 0)
            {
                Console.WriteLine("There's no word in this category, press any key to return to home page.");
                Console.ReadKey(true);
            }
            return order;
        }

        /// <summary>
        /// Print some information. 
        /// </summary>
        private static void GenericInfo()
        {
            Console.WriteLine("Input exit to leave this program,");
            Console.WriteLine("input home to go back to home page.");
            Console.WriteLine("Press enter to confirm your input.");
            Console.WriteLine("If you want to end your using, please use exit command, do not simply close the program,");
            Console.WriteLine($"if you do not exit program by this command, the learning record will not be saved.{Environment.NewLine}");
        }

        /// <summary>
        /// Save the learning record to save file of current thesaurus (see <see cref="GenericSetting.CurrentThesaurus"/>).
        /// </summary>
        private static void SaveLearningRecord()
        {
            FileStream saveFile = new FileStream($"{GenericSetting.CurrentThesaurus}/LearningRecord.dat", FileMode.Create);
            Formatter.Serialize(saveFile, WordsLearningAndRevisingCounts);
            saveFile.Dispose();
        }

        /// <summary>
        /// Generate question from current word, then react to user's inputs.
        /// </summary>
        /// <param name="order">The list that contains all words in this time's learning / revising's index in <see cref="Thesaurus"/>.</param>
        /// <param name="currentIndexInOrder">Current word index in list <paramref name="order"/>.</param>
        /// <param name="correctAnswer">The right answer of current word's question.</param>
        /// <param name="getInput">This method should return user's input.</param>
        /// <param name="reactionForWrongAnswer">Will be called is answer is wrong, if this method return true, current question will be skipped.</param>
        /// <param name="beforeGetInput">This method is optional, will be invoked before method <paramref name="getInput"/>.</param>
        /// <param name="additionalSkipCheck">
        /// <para>The parameter of it should be the right answer.</para>
        /// <para>If this method returns true,</para>
        /// <para>current question will be skip,</para>
        /// <para>this method should also do something like make this question appear later,</para>
        /// <para>or remove all same question.</para>
        /// </param>
        private static void Questioning(List<int> order, int currentIndexInOrder, string correctAnswer, Func<string> getInput, Func<bool> reactionForWrongAnswer, Action beforeGetInput = null, Func<bool> additionalSkipCheck = null)
        {
            while (Input != correctAnswer)
            {
                // invoke the method that should be called before get input, then get input, do skip check, and react to wrong answer if answer is wrong
                Console.Clear();
                beforeGetInput?.Invoke();
                Input = getInput();

                if (additionalSkipCheck != null && additionalSkipCheck())
                {
                    return;
                }

                if (Input != correctAnswer)
                {
                    if (reactionForWrongAnswer())
                    {
                        return;
                    }
                }
                else
                {
                    bool isLearning = CurrentCategory == WordCategory.Unlearned;

                    List<int> currentCountsList = isLearning ? WordsLearningAndRevisingCounts.Item1 : WordsLearningAndRevisingCounts.Item2;
                    List<int> currentIndexesList = isLearning ? UnlearnedWordIndexes : UnfamiliarWordIndexes;
                    List<int> nextIndexesList = isLearning ? UnfamiliarWordIndexes : FamiliarWordIndexes;

                    // revising count or learning count of current word ++
                    currentCountsList[order[currentIndexInOrder]]++;

                    Console.Write($"Right {(isLearning ? "spelling" : "answer")}, ");

                    if (currentIndexesList.Contains(order[currentIndexInOrder]) && currentCountsList[order[currentIndexInOrder]] >= (isLearning ? ThesaurusSetting.LearnTimePerWord : ThesaurusSetting.ReviseTimePerWord))
                    {
                        Console.WriteLine($"this word has been {(isLearning ? "learnt" : "revised")} equal to or more than {(isLearning ? ThesaurusSetting.LearnTimePerWord : ThesaurusSetting.ReviseTimePerWord)} times, press a to add it into {(isLearning ? "unfamiliar" : "familiar")} words,");

                        Console.WriteLine("press other key if you want to add it next time.");
                        if (Console.ReadKey(true).KeyChar == 'a')
                        {
                            currentIndexesList.Remove(order[currentIndexInOrder]);
                            nextIndexesList.Add(order[currentIndexInOrder]);
                        }
                        else
                        {
                            // do not add the word to nextIndexList, offset the ++
                            currentCountsList[order[currentIndexInOrder]]--;
                        }
                    }
                    else
                    {
                        if (currentIndexInOrder < order.Count - 1)
                        {
                            Console.WriteLine("press any key to continue.");
                            Console.ReadKey(true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// <para>In this method unfamiliar or familiar words will be revised by let user spell them,</para>
        /// <para>while user can only see the definitions of words.</para>
        /// </summary>
        /// <remarks>
        /// <para>Firstly, this method will call <see cref="ChoosingPage"/> to let user choose word category,</para>
        /// <para>then a list of words in current category will be initialized by method <see cref="WordOrderInit"/>,</para>
        /// <para>the words will be divided into several groups, group sizes are equal to <see cref="ThesaurusSetting.LearningGroupSize"/>.</para>
        /// <para>Next, the definitions of words will be shown to the user, user has to spell the word they think fit the current definition the best.</para>
        /// <para>If a word has been revised equal to <see cref="ThesaurusSetting.ReviseTimePerWord"/> times and is a unfamiliar word,</para>
        /// <para>user will be asked to add this word to familiar category now or next time.</para>
        /// <para>if user inputs home or exit, or all groups have been revised, learning record will be saved by <see cref="SaveLearningRecord"/>,</para>
        /// <para>then this method will return to <see cref="HomePage"/>.</para>
        /// </remarks>
        private static void SpellingMode()
        {
            string[] wordcategories = { "Unfamiliar", "Familiar" };
            string[] comments = { "Revise unfamiliar words.", "Revise familiar words." };
            int index = ChoosingPage(wordcategories, "Words", true, comments);

            CurrentCategory = (WordCategory)(index + 1);
            List<int> order = WordOrderInit();

            if (order.Count == 0)
            {
                return;
            }

            void QuestionInfo()
            {
                Console.WriteLine("Input the word that your think that fit the follow definition.");
                GenericInfo();
                Console.WriteLine(Thesaurus[order[index]].Value);
            }

            for (index = 0; index < order.Count; index++)
            {
                Questioning(order, index, Thesaurus[order[index]].Key, Console.ReadLine, () => 
                {
                    return ReactionForWrongAnswerWhenRevising(order, index, Thesaurus[order[index]].Key);
                }, QuestionInfo);
            }
            Console.WriteLine("this time's revising is finished, press any key to return to home page.");
            SaveLearningRecord();
            Console.ReadKey(true);
        }

        /// <summary>
        /// <para>Function as its name, it will be called in anonymous methods,</para>
        /// <para>those methods are parameters of <see cref="Questioning"/> in <see cref="SpellingMode"/> and <see cref="ChoosingMode"/>.</para>
        /// </summary>
        /// <param name="order">The revising order list, contains all indexes of words in this time's revising.</param>
        /// <param name="currentIndexInOrder">index of Current revising word's index in <paramref name="order"/></param>
        /// <param name="correctAnswer">The correct answer of current question.</param>
        /// <returns>Return true if the question should be skipped, otherwise return false.</returns>
        private static bool ReactionForWrongAnswerWhenRevising(List<int> order, int currentIndexInOrder, string correctAnswer)
        {
            Console.WriteLine("Wrong answer, press s to see the answer then skip this word, if you skip it, it will appear later,");
            Console.WriteLine("press other key to retry.");
            if (Console.ReadKey(true).KeyChar == 's')
            {
                Console.WriteLine($"The answer is {correctAnswer}, this word will appear later in this time's revising.");
                Console.WriteLine("Press any key to continue.");
                order.Insert(Rand.Next(currentIndexInOrder, order.Count + 1), order[currentIndexInOrder]);
                Console.ReadKey(true);
                return true;
            }
            return false;
        }

        /// <summary>
        /// <para>In this method unfamiliar or familiar words will be revised by show current word,</para>
        /// <para>its definition and definitions of three other words,</para>
        /// <para>user will be asked to choose the right definition.</para>
        /// </summary>
        /// <remarks>
        /// <para>Firstly, this method will call <see cref="ChoosingPage"/> to let user choose word category,</para>
        /// <para>then a list of words in current category will be initialized by method <see cref="WordOrderInit"/>,</para>
        /// <para>the words will be divided into several groups, group sizes are equal to <see cref="ThesaurusSetting.LearningGroupSize"/>.</para>
        /// <para>Next, the words will be shown with its definition and definitions of three other words,</para> 
        /// <para>the user will be asked to choose the right definition.</para>
        /// <para>If a word has been revised equal to <see cref="ThesaurusSetting.ReviseTimePerWord"/> times and is an unfamiliar word,</para>
        /// <para>user will be asked to add this word to familiar category now or next time.</para>
        /// <para>if user choose home or exit, or all groups have been revised, learning record will be saved by <see cref="SaveLearningRecord"/>,</para>
        /// <para>then this method will return to <see cref="HomePage"/>.</para>
        /// </remarks>
        private static void ChoosingMode()
        {
            string[] wordcategories = { "Unfamiliar", "Familiar" };
            string[] comments = { "Revise unfamiliar words.", "Revise familiar words." };
            string[] choices = { "A", "B", "C", "D" };
            
            string[] answers = new string[4];
            int index;
            int answerNo = 0;
            int wrongAnswer1Index, wrongAnswer2Index, wrongAnswer3Index;

            index = ChoosingPage(wordcategories, "Words", true, comments);

            CurrentCategory = (WordCategory)(index + 1);
            List<int> order = WordOrderInit();

            if (order.Count == 0)
            {
                return;
            }

            void QuestionInit()
            {
                do
                {
                    wrongAnswer1Index = Rand.Next(Thesaurus.Count);
                }
                while (wrongAnswer1Index == order[index]);

                do
                {
                    wrongAnswer2Index = Rand.Next(Thesaurus.Count);
                }
                while (wrongAnswer2Index == order[index] || wrongAnswer2Index == wrongAnswer1Index);

                do
                {
                    wrongAnswer3Index = Rand.Next(Thesaurus.Count);
                }
                while (wrongAnswer3Index == order[index] || wrongAnswer3Index == wrongAnswer2Index || wrongAnswer3Index == wrongAnswer1Index);

                // if some of answers are the same, the latter one will be chosen randomly again until all answer are different

                answers = new string[] {
                    Thesaurus[order[index]].Value,
                    Thesaurus[wrongAnswer1Index].Value,
                    Thesaurus[wrongAnswer2Index].Value,
                    Thesaurus[wrongAnswer3Index].Value
                };

                // answerNo will be assigned to a random no. in 0~3 before the call of QuestionInit()
                // switch answers[answerNo] and right answer to let answers[answerNo] be the right answer
                string temp = answers[0];
                answers[0] = answers[answerNo];
                answers[answerNo] = temp;
            }

            for (index = 0; index < order.Count; index++)
            {
                answerNo = Rand.Next(4);
                Questioning(order, index, ((char)(answerNo + 'A')).ToString(),
                    () =>
                    {
                        ChoosingPage(choices, "Options", true, answers, () => { Console.WriteLine($"Word: {Thesaurus[order[index]].Key}"); });
                        return Input;
                    },
                    () => { return ReactionForWrongAnswerWhenRevising(order, index, ((char)(answerNo + 'A')).ToString()); }, QuestionInit);
            }
            Console.WriteLine("this time's revising is finished, press any key to return to home page.");
            SaveLearningRecord();
            Console.ReadKey(true);
        }

        /// <summary>
        /// The page that user can choose non-study options.
        /// </summary>
        private static void OtherPage()
        {
            string[] otherPageOptions = { "Create Thesaurus", "Edit Settings", "Relearn Words" };
            string[] otherPageOptionComments = {
                    "Create new thesaurus.",
                    "Edit the settings of this program.",
                    "Move words from learned categories to unlearned category.",
                };

            ChoosingPage(otherPageOptions, "Options", true, otherPageOptionComments);

            switch (Input)
            {
                case "Create Thesaurus":
                    CreateThesaurus();
                    break;
                case "Edit Settings":
                    EditSettings();
                    break;
                case "Relearn Words":
                    RelearnWords();
                    break;
            }
        }


        /// <summary>
        /// <para>Generate new thesaurus file by well-formatted text file,</para>
        /// <para>then create a new directory that named by user and put the file in it.</para>
        /// </summary>
        private static void CreateThesaurus()
        {
            List<KeyValuePair<string, string>> newThesaurus = new List<KeyValuePair<string, string>>();
            StreamReader reader;

            Console.Clear();
            Console.WriteLine("Please input thesaurus text file name, for example: words.txt");
            Console.WriteLine("Text file should be placed in the directory where this program is located.");
            Console.WriteLine("Text file's format should be like this:");
            Console.WriteLine($"[Word1]{Environment.NewLine}[Definition for Word1]{Environment.NewLine}[Word2]{Environment.NewLine}[Definition for Word2]{Environment.NewLine}...{Environment.NewLine}");
            Console.WriteLine("If a word has two or more definitions, the format should be like this:");
            Console.WriteLine($"[Word1]{Environment.NewLine}1.[Definition1 for Word1]{Environment.NewLine}2.[Definition2 for Word1]{Environment.NewLine}[Word2]{Environment.NewLine}[Definition for Word2]{Environment.NewLine}...");
            Input = Console.ReadLine();
            try
            {
                reader = new StreamReader(Input);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("File not found, press any key to return to home page.");
                Console.ReadKey(true);
                return;
            }
            Input = reader.ReadLine();
            string temp;
            StringBuilder builder;
            while (Input != null)
            {
                builder = new StringBuilder();
                temp = Input;
                Input = reader.ReadLine();
                if (Input?.First() == '1')
                {
                    //there are multiple definitions of current word
                    do
                    {
                        builder.Append($"{Input}{Environment.NewLine}");
                        Input = reader.ReadLine();
                    }
                    while (Input?.First() >= '1' && Input?.First() <= '9');
                    // remove last empty line
                    builder.Remove(builder.Length - Environment.NewLine.Length, Environment.NewLine.Length);
                }
                else
                {
                    // there's only 1 definition of current word
                    builder.Append(Input);
                    Input = reader.ReadLine();
                }
                // add deciphered word and its deciphered definition
                newThesaurus.Add(new KeyValuePair<string, string>(Convert.ToBase64String(Encoding.Default.GetBytes(temp)), Convert.ToBase64String(Encoding.Default.GetBytes(builder.ToString()))));
            }
            reader.Dispose();

            // save the deciphered newThesaurus to file
            FileStream ThesaurusFile = new FileStream($"Thesaurus.dat", FileMode.Create);
            Formatter.Serialize(ThesaurusFile, newThesaurus);
            ThesaurusFile.Dispose();

            Console.Clear();
            do
            {
                Console.WriteLine("Thesaurus.dat is generated successfully,");
                Console.WriteLine("Please input name of the new thesaurus,");
                Console.WriteLine("Name should not contain space.");
                Console.WriteLine("Input exit or home if you want to place it manually.");
                Console.WriteLine("press enter to confirm your input.");
                Input = Console.ReadLine();

                if (Input.Contains(' '))
                {
                    Console.WriteLine("Thesaurus name should not contain space,");
                    Console.ReadKey(true);
                    Console.Clear();
                    continue;
                }

                if (ThesaurusList.Contains(Input))
                {
                    Console.WriteLine($"Thesaurus {Input} is already exist, press any key to retry.");
                    Console.ReadKey(true);
                    Console.Clear();
                    continue;
                }
            } while (ThesaurusList.Contains(Input) || Input.Contains(' '));

            if (!Directory.Exists(Input))
            {
                Directory.CreateDirectory(Input);
            }
            
            FileInfo file = new FileInfo("Thesaurus.dat");
            file.MoveTo(Input);
            Console.WriteLine($"Thesaurus {Input} is created successfully, press any key to return to home page.");
            Console.ReadKey(true);
        }

        /// <summary>
        /// Let user change settings of this program then save the settings.
        /// </summary>
        /// <remarks>
        /// <para>Firstly, this method will call <see cref="ChoosingPage"/> to let user choose setting category,</para>
        /// <para>then call <see cref="ChoosingPage"/> target on specific setting.</para>
        /// <para>User can choose exit or home to leave this program or return to home page in those two steps.</para>
        /// <para>Next, if the setting has attribute "AcceptableValueAttribute",</para>
        /// <para>this method will call <see cref="ChoosingPage"/> to let user choose value of this setting.</para>
        /// <para>if the setting doesn't have this attribute, then it will be checked for attribute "ValueRangeAttribute",</para>
        /// <para>if this attribute is found, this method will let user increases / decreases the value by arrow keys and confirm by enter.</para>
        /// <para>If those two attributes cannot be found, this method will simply receive user's input by typing.</para>
        /// <para>After getting and parsing user's input, those setting will be set to input value,</para>
        /// <para>if the changing value like background color,</para>
        /// <para>this method will call method <see cref="SetColor"/>.</para>
        /// <para>At last, the changed setting will be saved to current setting file by <see cref="SaveSettingFile"/>.</para>
        /// </remarks>
        private static void EditSettings()
        {
            string[] settings = { "Generic Settings", "Thesaurus Settings" };
            string[] settingComments = { "Settings of all thesaurus.", "Setting of current thesaurus." };
            
            void UnrestrictedSettingInfo()
            {
                Console.WriteLine("Input home to return to home page,");
                Console.WriteLine("input exit to leave this program,");
                Console.WriteLine("input value to change this setting to your inputed value,");
                Console.WriteLine("press enter to confirm your input.");
            }

            ChoosingPage(settings, "Settings", true, settingComments);

            bool isGenericSetting = Input == "Generic Settings";
            FieldInfo[] fields = (isGenericSetting ? typeof(GenericSetting) : typeof(ThesaurusSetting)).GetFields(BindingFlags.Public | BindingFlags.Static);

            StringBuilder builder;

            string[] fieldNames = Array.ConvertAll(fields, (field) => { return field.Name; });
            string[] fieldComments = Array.ConvertAll(fields, (field) =>
            {
                builder = new StringBuilder();
                Array.ForEach(field.GetCustomAttributes().ToArray(), (attr) => { builder.Append($"{attr}{Environment.NewLine}"); });
                builder.Append($"Current value = {field.GetValue(null)}.");
                return builder.ToString();
            });

            int index = ChoosingPage(fieldNames, "Settings", true, fieldComments);

            // if switch thesaurus, save generic settings to file then initialize the thesaurus, then return
            if (Input == "CurrentThesaurus")
            {
                ChoosingPage(ThesaurusList, "Thesauruses", true);

                GenericSetting.CurrentThesaurus = Input;
                SaveSettingFile(true, fields);
                Console.Clear();
                ThesaurusInit();
                Console.WriteLine($"Current thesaurus is {GenericSetting.CurrentThesaurus} now, press any key to return to home page.");
                Console.ReadKey(true);
                return;
            }

            ValueRangeAttribute range = fields[index].GetCustomAttribute<ValueRangeAttribute>();
            AcceptableValueAttribute values = fields[index].GetCustomAttribute<AcceptableValueAttribute>();

            if (values != null)
            {
                // acceptable values are found, so call ChoosingPage() to let user choose a value
                string[] availableValueStrings = Array.ConvertAll(values.AcceptableValues, (obj) => { return obj.ToString(); });
                int value = ChoosingPage(availableValueStrings, "Available values", true, null, null, fieldNames[index] == "BackGroundColor" ? Array.ConvertAll(values.AcceptableValues, (obj) => { return (ConsoleColor)obj; }) : null, fieldNames[index] == "FontColor" ? Array.ConvertAll(values.AcceptableValues, (obj) => { return (ConsoleColor)obj; }) : null);

                // set the value to chosen value
                fields[index].SetValue(null, values.AcceptableValues[value]);
            }

            if (range != null)
            {
                int value = (int)fields[index].GetValue(null);
                Console.Clear();
                Console.WriteLine(fieldComments[index]);
                Console.WriteLine($"{fieldNames[index]}:");
                Console.WriteLine(value);
                Console.Write("Press left and right to increase and decrease the value, press enter to confirm the value.");
                Console.CursorTop--;
                Console.CursorLeft = 0;
                ConsoleKey key;
                while ((key = Console.ReadKey(true).Key) != ConsoleKey.Enter)
                {
                    if (key == ConsoleKey.LeftArrow)
                    {
                        value--;
                        if (value < range.MinValue)
                        {
                            value = range.MaxValue;
                        }
                        Console.Write($"{value}\t");
                        Console.CursorLeft = 0;
                    }

                    if (key == ConsoleKey.RightArrow)
                    {
                        value++;
                        if (value > range.MaxValue)
                        {
                            value = range.MinValue;
                        }
                        Console.Write($"{value}\t");
                        Console.CursorLeft = 0;
                    }
                }
                fields[index].SetValue(null, value);
            }

            if (values == null && range == null)
            {
                Console.Clear();
                UnrestrictedSettingInfo();
                Input = Console.ReadLine();

                if (fields[index].FieldType == typeof(string))
                {
                    fields[index].SetValue(null, Input);
                }

                if (fields[index].FieldType == typeof(int))
                {
                    bool isParsingError;
                    int temp;
                    do
                    {
                        // if parsing error, let user retry until input is available
                        if (isParsingError = !int.TryParse(Input, out temp))
                        {
                            Console.WriteLine("Input is not a integer, press any key to retry.");
                            Console.ReadKey(true);
                            Console.Clear();
                            UnrestrictedSettingInfo();
                            Input = Console.ReadLine();
                        }
                    }
                    while (isParsingError);
                    fields[index].SetValue(null, temp);
                }
            }

            // if change window size, set window size to avoid display error
            if (fieldNames[index] == "WindowSize")
            {
                Console.SetWindowSize(WindowWidth, WindowHeight);
                Console.SetBufferSize(WindowWidth, 80);
            }

            // make the learning counts and revising counts increase to avoid familiar or unfamiliar words become unlearned words
            if (fieldNames[index] == "LearnTimePerWord" || fieldNames[index] == "ReviseTimePerWord")
            {
                UnfamiliarWordIndexes.ForEach((i) => { WordsLearningAndRevisingCounts.Item1[i] = Math.Max(WordsLearningAndRevisingCounts.Item1[i], ThesaurusSetting.LearnTimePerWord); });
                FamiliarWordIndexes.ForEach((i) => 
                {
                    WordsLearningAndRevisingCounts.Item1[i] = Math.Max(WordsLearningAndRevisingCounts.Item1[i], ThesaurusSetting.LearnTimePerWord);
                    WordsLearningAndRevisingCounts.Item2[i] = Math.Max(WordsLearningAndRevisingCounts.Item2[i], ThesaurusSetting.ReviseTimePerWord);
                });
                SaveLearningRecord();
            }

            // if color change, SetColor() has to be called, otherwise move the cursor to the right place
            if (fieldNames[index] == "BackGroundColor" || fieldNames[index] == "FontColor")
            {
                SetColor();
            }
            else
            {
                Console.CursorTop += 2;
            }

            SaveSettingFile(isGenericSetting, fields);
            Console.WriteLine("Setting changing has been saved, press any key to return to home page.");
            Console.ReadKey(true);
        }

        /// <summary>
        /// choose a word from learned words, then move it into unlearned category.
        /// </summary>
        /// <remarks>
        /// <para>All the unfamiliar words and familiar words will be shown in method <see cref="ChoosingPage"/> with their category name, revise count and definition.</para>
        /// <para>then, if the chosen option is exit or home, this method will leave this program or return to home page,</para>
        /// <para>otherwise the chosen word will be moved from its recent category to unlearned category,</para>
        /// <para>its learning count and revising count will be cleared, then learning record will be save by <see cref="SaveLearningRecord"/>.</para>
        /// </remarks>
        private static void RelearnWords()
        {
            // find all learned words
            List<int> learnedWordIndexes = new List<int>(UnfamiliarWordIndexes.Count + FamiliarWordIndexes.Count);
            learnedWordIndexes.AddRange(UnfamiliarWordIndexes);
            learnedWordIndexes.AddRange(FamiliarWordIndexes);

            string[] words = Array.ConvertAll(learnedWordIndexes.ToArray(), (i) => { return Thesaurus[i].Key; });
            string[] comments = Array.ConvertAll(learnedWordIndexes.ToArray(), (i) => {
                // return familiar or unfamiliar, if unfamiliar append revise count, at last append definition at next line
                return $"{(WordsLearningAndRevisingCounts.Item2[i] >= ThesaurusSetting.ReviseTimePerWord ? "Familiar" : $"Unfamiliar, Revise Time: {WordsLearningAndRevisingCounts.Item2[i]}")}{Environment.NewLine}{Thesaurus[i].Value}";
            });

            int index = ChoosingPage(words, "Words", true, comments, () => { Console.WriteLine("Move the chosen word from learned categories to unlearned category."); });
            
            // clear learning count and revising count, then add it into unlearned category
            WordsLearningAndRevisingCounts.Item1[learnedWordIndexes[index]] = 0;
            WordsLearningAndRevisingCounts.Item2[learnedWordIndexes[index]] = 0;
            (index < UnfamiliarWordIndexes.Count ? UnfamiliarWordIndexes : FamiliarWordIndexes).RemoveAt(index % UnfamiliarWordIndexes.Count);
            UnlearnedWordIndexes.Add(learnedWordIndexes[index]);

            SaveLearningRecord();
            Console.WriteLine($"Word \"{Thesaurus[learnedWordIndexes[index]].Key}\" has been moved from {(index < UnfamiliarWordIndexes.Count ? "un" : "")}familiar category to unlearned category, press any key to return to home page.");
            Console.ReadKey(true);
        }
    }
}
