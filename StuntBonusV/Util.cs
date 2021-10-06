using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.UI;
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
            Notification.Show(message, blinking);
        }

        /// <summary>
        /// Shows a subtitle at the bottom of the screen for a given time
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="duration">The duration to display the subtitle in milliseconds.</param>
        /// <param name="drawsImmediately">if set to <c>true</c> the message will be drawn immediately; otherwise, the message will be drawn after the previous subtitle has finished.</param>
        internal static void ShowSubtitle(string message, int duration = 2500, bool drawsImmediately = true)
        {
            Screen.ShowSubtitle(message, duration, drawsImmediately);
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

    internal static class MathUtil
    {
        public static double MeterToFeet(double meter) => meter * 0.3048;
    }
}
