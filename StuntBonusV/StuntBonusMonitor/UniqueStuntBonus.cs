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
        internal class UniqueStuntBonus : StuntBonusScript
        {
            #region setting

            class UniqueStuntSetting : Setting
            {
                public int BaseAward { get; set; } = 500;
                public bool EnableBonusX { get; set; } = true;
                public bool EnableLastSpecialAward { get; set; } = true;
                public int LastAward { get; set; } = 250000;
                public bool UseNotificationsToShowResult { get; set; } = false;

                public override bool Validate()
                {
                    if (BaseAward <= 0) return false;
                    if (EnableLastSpecialAward && LastAward <= 0) return false;
                    return true;
                }
            }
            public override string SettingFileName { get; } = "UniqueStuntBonus.xml";

            #endregion

            int _completedUniqueStuntCount = GtaNativeUtil.GetCompletedUniqueStuntCount();

            protected override void Setup()
            {
                Tick += OnTick;
            }

            internal void OnTick(object o, EventArgs e)
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
