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
            bool _isSkiingWithTwoWheels;
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
                if (!player.SafeExists()) { return; }
                var playerVeh = Game.Player.Character.CurrentVehicle;

                if (playerVeh.SafeExists() && (!_currentVehicle.SafeExists() || _currentVehicle != playerVeh))
                {
                    _currentVehicle = playerVeh;
                    _isPerformingSkiing = false;
                }
                else if (!_currentVehicle.SafeExists() || !player.IsInVehicle(_currentVehicle))
                {
                    _currentVehicle = null;
                    _isPerformingSkiing = false;
                    return;
                }

                if (_currentVehicle.SafeExists())
                {
                    if (_currentVehicle.IsAlive && _currentVehicle.IsInSkiingStunt(out var isSkiingWithTwoWheels))
                    {
                        if (!_isPerformingSkiing)
                        {
                            _isPerformingSkiing = true;
                            _isSkiingWithTwoWheels = isSkiingWithTwoWheels;
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

                            var resultStyle = UseNotificationsToShowResult ? ShowingResultStyle.Notification : ShowingResultStyle.Subtitle;
                            if (Game.Language == Language.Japanese)
                            {
                                    ShowResult(string.Format("片輪走行ボーナス {0}ドル 距離:{1:F2}m 時間:{2}秒", bonusMoney, _TotalSkiingDistance, timeSecs), resultStyle, 3000);
                            }
                            else
                            {
                                if (_isSkiingWithTwoWheels)
                                    ShowResult($"TWO WHEELS DOUBLE BONUS: ${bonusMoney} Distance: {_TotalSkiingDistance:F2}m Time: {timeSecs} seconds", resultStyle, 3000);
                                else
                                    ShowResult($"SKIING BONUS: ${bonusMoney} Distance: {_TotalSkiingDistance:F2}m Time: {timeSecs} seconds", resultStyle, 3000);
                            }
                        }
                    }
                }
            }

        }
    }
}
