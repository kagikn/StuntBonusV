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
        public class StoppieBonus : StuntBonusScript
        {
            #region setting

            public class StoppieBonusSetting : Setting
            {
                public bool UseNotificationsToShowResult { get; set; } = false;
                public override bool Validate()
                {
                    return true;
                }
            }
            public override string SettingFileName { get; } = "StoppieBonus.xml";

            #endregion

            #region fields
            private StoppieBonusSetting _setting;

            Vehicle _currentVehicle;
            Vector3 _prevVehiclePos;
            uint _startTimeOfStoppie;
            bool _isPerformingStoppie;
            float _TotalStoppieDistance;
            const int MIN_TIME_TO_EARN_MONEY = 2000;

            bool UseNotificationsToShowResult => _setting.UseNotificationsToShowResult;
            #endregion fields

            protected override void Setup()
            {
                Tick += OnTick;

                _setting = LoadSetting<StoppieBonusSetting>();
                if (_setting == null || !_setting.Validate())
                {
                    _setting = new StoppieBonusSetting();
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
                    _isPerformingStoppie = false;
                }
                else if (!_currentVehicle.SafeExists() || !player.IsInVehicle(_currentVehicle))
                {
                    _currentVehicle = null;
                    _isPerformingStoppie = false;
                    return;
                }

                if (_currentVehicle.SafeExists())
                {
                    if (_currentVehicle.IsAlive && _currentVehicle.IsInStoppie())
                    {
                        if (!_isPerformingStoppie)
                        {
                            _isPerformingStoppie = true;
                            _startTimeOfStoppie = (uint)Game.GameTime;
                            _TotalStoppieDistance = 0f;
                            _prevVehiclePos = _currentVehicle.Position;
                        }

                        _TotalStoppieDistance += Vector3.Distance(_currentVehicle.Position, _prevVehiclePos);
                        _prevVehiclePos = _currentVehicle.Position;
                    }
                    else if (_isPerformingStoppie)
                    {
                        _isPerformingStoppie = false;

                        var stoppieTime = (uint)Game.GameTime - _startTimeOfStoppie;
                        if (stoppieTime >= MIN_TIME_TO_EARN_MONEY)
                        {
                            var bonusMoney = ((int)_TotalStoppieDistance) / 2;
                            Game.Player.Money += bonusMoney;

                            var timeSecs = stoppieTime / 1000;

                            var resultStyle = UseNotificationsToShowResult ? ShowingResultStyle.Notification : ShowingResultStyle.Subtitle;
                            if (Game.Language == Language.Japanese)
                            {
                                ShowResult($"ジャックナイフボーナス {bonusMoney}ドル 距離:{_TotalStoppieDistance:F2}m 時間:{timeSecs}秒", resultStyle, 3000);
                            }

                            else
                            {
                                ShowResult($"STOPPIE BONUS: ${bonusMoney} Distance: {_TotalStoppieDistance:F2}m Time: {timeSecs} seconds", resultStyle, 3000);
                            }
                        }
                    }
                }
            }

        }
    }
}
