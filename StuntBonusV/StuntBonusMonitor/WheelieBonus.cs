﻿using System;
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
        public class WheelieBonus : StuntBonusScript
        {
            #region setting

            public class WheelieBonusSetting : Setting
            {
                public bool UseNotificationsToShowResult { get; set; } = false;
                public override bool Validate()
                {
                    return true;
                }
              
            }
            public override string SettingFileName { get; } = "WheelieBonus.xml";

            #endregion

            #region fields
            private WheelieBonusSetting _setting;

            Vehicle _currentVehicle;
            Vector3 _prevVehiclePos;
            uint _startTimeOfWheelie;
            bool _isPerformingWheelie;
            float _TotalWheelieDistance;
            const int MIN_TIME_TO_EARN_MONEY = 5000;

            bool UseNotificationsToShowResult => _setting.UseNotificationsToShowResult;
            #endregion fields

            protected override void Setup()
            {
                Tick += OnTick;

                _setting = LoadSetting<WheelieBonusSetting>();
                if (_setting == null || !_setting.Validate())
                {
                    _setting = new WheelieBonusSetting();
                    SaveSetting(_setting);
                }
            }

            internal void OnTick(object o, EventArgs e)
            {
                var player = Game.Player.Character;
                if (!player.ExistsSafe()) { return; }
                var playerVeh = Game.Player.Character.CurrentVehicle;

                if (playerVeh.ExistsSafe() && _currentVehicle != playerVeh)
                {
                    _currentVehicle = playerVeh;
                    _isPerformingWheelie = false;
                }
                else if (!_currentVehicle.ExistsSafe())
                {
                    _currentVehicle = null;
                    _isPerformingWheelie = false;
                    return;
                }

                if (_currentVehicle.ExistsSafe() && player.IsInVehicle(_currentVehicle))
                {
                    if (_currentVehicle.IsAlive && _currentVehicle.IsInWheelie())
                    {
                        if (!_isPerformingWheelie)
                        {
                            _isPerformingWheelie = true;
                            _startTimeOfWheelie = (uint)Game.GameTime;
                            _TotalWheelieDistance = 0f;
                            _prevVehiclePos = _currentVehicle.Position;
                        }

                        _TotalWheelieDistance += Vector3.Distance(_currentVehicle.Position, _prevVehiclePos);
                        _prevVehiclePos = _currentVehicle.Position;
                    }
                    else if (_isPerformingWheelie)
                    {
                        _isPerformingWheelie = false;

                        var wheelieTime = (uint)Game.GameTime - _startTimeOfWheelie;
                        if (wheelieTime >= MIN_TIME_TO_EARN_MONEY)
                        {
                            var bonusMoney = (((int)wheelieTime / 1000) + (int)_TotalWheelieDistance) / 2;
                            bonusMoney *= 2;
                            bonusMoney /= 5;
                            bonusMoney /= 2;
                            player.Money += bonusMoney;

                            var timeSecs = wheelieTime / 1000;
                            var timeMillisecs = wheelieTime - timeSecs * 1000;

                            var resultStyle = UseNotificationsToShowResult ? ShowingResultStyle.Notification : ShowingResultStyle.Subtitle;
                            if (Game.Language == Language.Japanese)
                            {
                                ShowResult(String.Format("ウィリーボーナス！ {0}ドル 距離:{1}m 時間:{2}.{3}秒", bonusMoney, _TotalWheelieDistance, timeSecs, timeMillisecs), resultStyle, 3000);
                            }
                            else
                            {
                                ShowResult(String.Format("WHEELIE BONUS: ${0} Distance: {1}m Time: {2}.{3} seconds", bonusMoney, _TotalWheelieDistance, timeSecs, timeMillisecs), resultStyle, 3000);
                            }
                        }
                    }
                }
            }
        }
    }
}
