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
        internal class SkiingBonus : StuntBonusScript
        {
            #region setting

            class SkiingBonusSetting : Setting
            {
                public override bool Validate()
                {
                    return true;
                }
            }
            public override string SettingFileName { get; } = "SkiingBonus.xml";

            #endregion

            static Vehicle _currentVehicle;
            static Vector3 _prevVehiclePos;
            static uint _startTimeOfSkiing;
            static bool _isPerformingSkiing;
            static float _TotalSkiingDistance;
            const int MIN_TIME_TO_EARN_MONEY = 2000;

            protected override void Setup()
            {
                Tick += OnTick;
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
                            player.Money += bonusMoney;

                            var timeSecs = skiingTime / 1000;
                            var timeMillisecs = skiingTime - timeSecs * 1000;
                            if (Game.Language == Language.Japanese)
                            {
                                GtaNativeUtil.ShowSubtitle(String.Format("片輪走行ボーナス！ {0}ドル 距離:{1}m 時間:{2}.{3}秒", bonusMoney, _TotalSkiingDistance, timeSecs, timeMillisecs), 3000, false);
                            }
                            else
                            {
                                GtaNativeUtil.ShowSubtitle(String.Format("SKIING BONUS: ${0} Distance: {1}m Time: {2}.{3} seconds", bonusMoney, _TotalSkiingDistance, timeSecs, timeMillisecs), 3000, false);
                            }
                        }
                    }
                }
            }

        }
    }
}
