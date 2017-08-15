using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using GTA;
using GTA.Native;
using GTA.Math;

namespace StuntBonusV
{
    internal static partial class StuntBonusMonitor
    {
        internal static class UniqueStuntBonus
        {
            static int _completedUniqueStuntCount = GtaNativeUtil.GetCompletedUniqueStuntCount();

            internal static void OnTick(object o, EventArgs e)
            {
                var currentCompletedStuntJumpCount = GtaNativeUtil.GetCompletedUniqueStuntCount();
                if (_completedUniqueStuntCount < currentCompletedStuntJumpCount)
                {
                    Game.Player.Money += 500;
                    _completedUniqueStuntCount = currentCompletedStuntJumpCount;
                }
            }
        }
    }
}
