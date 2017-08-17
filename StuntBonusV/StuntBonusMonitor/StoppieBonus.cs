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
        internal class StoppieBonus : StuntBonusScript
        {
            #region setting

            class StoppieBonusSetting : Setting
            {
                public bool UseNotificationsToShowResult { get; set; } = false;
                public override bool Validate()
                {
                    return true;
                }
            }
            public override string SettingFileName { get; } = "StoppieBonus.xml";

            #endregion

            private StoppieBonusSetting _setting;

            Vehicle _currentVehicle;
            Vector3 _prevVehiclePos;
            uint _startTimeOfStoppie;
            bool _isPerformingStoppie;
            float _TotalStoppieDistance;
            const int MIN_TIME_TO_EARN_MONEY = 2000;

            protected override void Setup()
            {
                Tick += OnTick;

                var _setting = LoadSetting<StoppieBonusSetting>();
                if (_setting == null || !_setting.Validate())
                {
                    _setting = new StoppieBonusSetting();
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
                    _isPerformingStoppie = false;
                }
                else if (!_currentVehicle.ExistsSafe())
                {
                    _currentVehicle = null;
                    _isPerformingStoppie = false;
                    return;
                }

                if (_currentVehicle.ExistsSafe() && player.IsInVehicle(_currentVehicle))
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
                            player.Money += bonusMoney;

                            var timeSecs = stoppieTime / 1000;
                            var timeMillisecs = stoppieTime - timeSecs * 1000;
                            if (Game.Language == Language.Japanese)
                            {
                                GtaNativeUtil.ShowSubtitle(String.Format("ジャックナイフボーナス！ {0}ドル 距離:{1}m 時間:{2}.{3}秒", bonusMoney, _TotalStoppieDistance, timeSecs, timeMillisecs), 3000, false);
                            }
                            else
                            {
                                GtaNativeUtil.ShowSubtitle(String.Format("STOPPIE BONUS: ${0} Distance: {1}m Time: {2}.{3} seconds", bonusMoney, _TotalStoppieDistance, timeSecs, timeMillisecs), 3000, false);
                            }
                        }
                    }
                }
            }

        }
    }
}
