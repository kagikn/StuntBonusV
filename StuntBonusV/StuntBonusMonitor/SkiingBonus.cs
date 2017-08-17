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
        public class SkiingBonus : StuntBonusScript
        {
            #region setting

            public class SkiingBonusSetting : Setting
            {
                public bool UseNotificationsToShowResult { get; set; } = false;
                public override bool Validate()
                {
                    return true;
                }
            }
            public override string SettingFileName { get; } = "SkiingBonus.xml";

            #endregion

            #region fields
            private SkiingBonusSetting _setting;

            Vehicle _currentVehicle;
            Vector3 _prevVehiclePos;
            uint _startTimeOfSkiing;
            bool _isPerformingSkiing;
            float _TotalSkiingDistance;
            const int MIN_TIME_TO_EARN_MONEY = 2000;

            bool UseNotificationsToShowResult => _setting.UseNotificationsToShowResult;
            #endregion fields

            protected override void Setup()
            {
                Tick += OnTick;

                _setting = LoadSetting<SkiingBonusSetting>();
                if (_setting == null || !_setting.Validate())
                {
                    _setting = new SkiingBonusSetting();
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
                    _isPerformingSkiing = false;
                }
                else if (!_currentVehicle.ExistsSafe())
                {
                    _currentVehicle = null;
                    _isPerformingSkiing = false;
                    return;
                }

                if (_currentVehicle.ExistsSafe() && player.IsInVehicle(_currentVehicle))
                {
                    if (_currentVehicle.IsAlive && _currentVehicle.IsInSkiingStunt())
                    {
                        if (!_isPerformingSkiing)
                        {
                            _isPerformingSkiing = true;
                            _startTimeOfSkiing = (uint)Game.GameTime;
                            _TotalSkiingDistance = 0f;
                            _prevVehiclePos = _currentVehicle.Position;
                        }

                        _TotalSkiingDistance += Vector3.Distance(_currentVehicle.Position, _prevVehiclePos);
                        _prevVehiclePos = _currentVehicle.Position;
                    }
                    else if (_isPerformingSkiing)
                    {
                        _isPerformingSkiing = false;

                        var skiingTime = (uint)Game.GameTime - _startTimeOfSkiing;
                        if (skiingTime >= MIN_TIME_TO_EARN_MONEY)
                        {
                            var bonusMoney = (((int)skiingTime / 1000) + (int)_TotalSkiingDistance) / 2;
                            Game.Player.Money += bonusMoney;

                            var timeSecs = skiingTime / 1000;
                            var timeMillisecs = skiingTime - timeSecs * 1000;

                            var resultStyle = UseNotificationsToShowResult ? ShowingResultStyle.Notification : ShowingResultStyle.Subtitle;
                            if (Game.Language == Language.Japanese)
                            {
                                ShowResult(String.Format("片輪走行ボーナス！ {0}ドル 距離:{1}m 時間:{2}.{3}秒", bonusMoney, _TotalSkiingDistance, timeSecs, timeMillisecs), resultStyle, 3000);
                            }
                            else
                            {
                                ShowResult(String.Format("SKIING BONUS: ${0} Distance: {1}m Time: {2}.{3} seconds", bonusMoney, _TotalSkiingDistance, timeSecs, timeMillisecs), resultStyle, 3000);
                            }
                        }
                    }
                }
            }

        }
    }
}
