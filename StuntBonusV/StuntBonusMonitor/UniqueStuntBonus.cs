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
    public static partial class StuntBonusMonitor
    {
        public class UniqueStuntBonus : StuntBonusScript
        {
            #region setting

            public class UniqueStuntSetting : Setting
            {
                public int BaseAward { get; set; } = 500;
                public bool IsBonusXEnabled { get; set; } = true;
                public bool IsLastSpecialAwardEnabled { get; set; } = true;
                public int LastStuntAward { get; set; } = 250000;

                public override bool Validate()
                {
                    if (BaseAward <= 0) return false;
                    if (IsLastSpecialAwardEnabled && LastStuntAward <= 0) return false;
                    return true;
                }
            }
            public override string SettingFileName { get; } = "UniqueStuntBonus.xml";

            #endregion

            #region fields
            private UniqueStuntSetting _setting;

            private int _completedUniqueStuntCount = GtaNativeUtil.GetCompletedUniqueStuntCount();

            private int BaseAward => _setting.BaseAward;
            private bool IsBonusXEnabled => _setting.IsBonusXEnabled;
            private bool IsLastSpecialAwardEnabled => _setting.IsLastSpecialAwardEnabled;
            private int LastStuntAward => _setting.LastStuntAward;
            #endregion fields

            protected override void Setup()
            {
                Tick += OnTick;

                _setting = LoadSetting<UniqueStuntSetting>();
                if (_setting == null || !_setting.Validate())
                {
                    _setting = new UniqueStuntSetting();
                    SaveSetting(_setting);
                }
            }

            internal void OnTick(object o, EventArgs e)
            {
                var currentCompletedStuntJumpCount = GtaNativeUtil.GetCompletedUniqueStuntCount();
                if (_completedUniqueStuntCount < currentCompletedStuntJumpCount)
                {
                    var bonusMoney = BaseAward;
                    if (IsLastSpecialAwardEnabled && _completedUniqueStuntCount == 50)
                    {
                        bonusMoney = LastStuntAward;
                    }
                    else if (IsBonusXEnabled)
                    {
                        bonusMoney *= currentCompletedStuntJumpCount;
                    }

                    Game.Player.Money += bonusMoney;
                    _completedUniqueStuntCount = currentCompletedStuntJumpCount;
                }
            }
        }
    }
}
