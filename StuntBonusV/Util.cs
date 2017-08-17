using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Native;
using GTA.Math;

namespace StuntBonusV
{
    internal static class GtaNativeUtil
    {
        const int USJS_COMPLETED_HASH = unchecked((int)0x861ADACB);

        /// <summary>
        /// Creates a notification above the minimap with the given message.
        /// </summary>
        /// <param name="message">The message in the notification.</param>
        /// <param name="blinking">if set to <c>true</c> the notification will blink.</param>
        public static void ShowNotification(string message, bool blinking = false)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Function.Call(Hash._SET_NOTIFICATION_TEXT_ENTRY, "CELL_EMAIL_BCON");
            const int maxBytes = 99;

            foreach (var str in Util.ToSlicedStrings(message, maxBytes))
            {
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, str);
            }

            Function.Call(Hash._DRAW_NOTIFICATION, blinking, true);
        }

        /// <summary>
        /// Shows a subtitle at the bottom of the screen for a given time
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="duration">The duration to display the subtitle in milliseconds.</param>
        /// <param name="drawsImmediately">if set to <c>true</c> the message will be drawn immediately; otherwise, the message will be drawn after the previous subtitle has finished.</param>
        internal static void ShowSubtitle(string message, int duration = 2500, bool drawsImmediately = true)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Function.Call(Hash._SET_TEXT_ENTRY_2, "CELL_EMAIL_BCON");
            const int maxBytes = 99;

            foreach (var str in Util.ToSlicedStrings(message, maxBytes))
            {
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, str);
            }

            Function.Call(Hash._DRAW_SUBTITLE_TIMED, duration, drawsImmediately);
        }

        internal static int GetCompletedUniqueStuntCount()
        {
            unsafe
            {
                int tmpInt = 0;
                Function.Call(Hash.STAT_GET_INT, USJS_COMPLETED_HASH, &tmpInt, -1);
                return tmpInt;
            }
        }
    }

    internal static class Util
    {
        internal static string DllPath { get; } = Path.GetDirectoryName((new Uri(Assembly.GetExecutingAssembly().CodeBase)).LocalPath);
        internal static string SettingRootPath { get; } = DllPath + Path.DirectorySeparatorChar + "StuntBonusV";

        internal static string[] ToSlicedStrings(string input, int maxByteLengthPerString = 99)
        {
            var utf8ByteCount = Encoding.UTF8.GetByteCount(input);

            if (utf8ByteCount == input.Length)
            {
                return ToSlicedForAscii(input, maxByteLengthPerString);
            }
            else
            {
                return ToSlicedForMultibyteStr(input, maxByteLengthPerString);
            }
        }

        private static string[] ToSlicedForAscii(string input, int maxByteLengthPerString = 99)
        {
            var initListCapacity = input.Length / maxByteLengthPerString;
            if (input.Length % maxByteLengthPerString > 0)
            {
                initListCapacity += 1;
            }

            var stringArray = new string[initListCapacity];

            for (int i = 0; i < initListCapacity; i++)
            {
                stringArray[i] = (input.Substring(i * maxByteLengthPerString, System.Math.Min(maxByteLengthPerString, input.Length - i * maxByteLengthPerString)));
            }

            return stringArray;
        }

        private static string[] ToSlicedForMultibyteStr(string input, int maxByteLengthPerString = 99)
        {
            if (maxByteLengthPerString < 0)
            {
                throw new ArgumentOutOfRangeException("maxLengthPerString");
            }
            if (string.IsNullOrEmpty(input) || maxByteLengthPerString == 0)
            {
                return new string[0];
            }

            var enc = Encoding.UTF8;

            var utf8ByteCount = enc.GetByteCount(input);
            if (utf8ByteCount <= maxByteLengthPerString)
            {
                return new string[] { input };
            }

            var initListCapacity = utf8ByteCount / maxByteLengthPerString;
            if (utf8ByteCount % maxByteLengthPerString > 0)
            {
                initListCapacity += 1;
            }

            var stringList = new List<string>(initListCapacity);
            var startIndex = 0;

            for (int i = 0; i <= input.Length; i++)
            {
                var length = i - startIndex;
                if (enc.GetByteCount(input.Substring(startIndex, length)) > maxByteLengthPerString)
                {
                    stringList.Add(input.Substring(startIndex, length - 1));
                    startIndex = i - 1;
                }
            }
            stringList.Add(input.Substring(startIndex, input.Length - startIndex));

            return stringList.ToArray();
        }
    }
}
