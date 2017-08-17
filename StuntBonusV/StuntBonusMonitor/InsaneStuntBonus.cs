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
        public class InsaneStuntBonus : StuntBonusScript
        {
            #region setting

            public class InsaneStuntBonusSetting : Setting
            {
                public bool IsPerfectLandingDetectionEnabled { get; set; } = true;
                public float PerfectLandingHealthThreshold { get; set; } = 10f;
                public bool UseNotificationsToShowResult { get; set; } = false;
                public override bool Validate()
                {
                    return PerfectLandingHealthThreshold > 0f;
                }
            }
            public override string SettingFileName { get; } = "InsaneStuntBonus.xml";

            #endregion

            #region fields
            private InsaneStuntBonusSetting _setting;

            private float _totalHeadingRotation = 0;
            private float _prevVehicleHeading = 0;
            private uint _flipCount = 0;
            private bool _wasFlippedInPrevFrame = true;
            private bool _isStunting = false;
            private float _maxVehicleZPos = float.MinValue;
            private Vehicle _currentVehicle = null;
            private Vector3 _currentVehiclePos = Vector3.Zero;
            private Vector3 _initVehiclePos = Vector3.Zero;

            bool IsPerfectLandingDetectionEnabled => _setting.IsPerfectLandingDetectionEnabled;
            float PerfectLandingHealthThreshold => _setting.PerfectLandingHealthThreshold;
            bool UseNotificationsToShowResult => _setting.UseNotificationsToShowResult;

            List<InsaneStuntBonusResult> StuntResults = new List<InsaneStuntBonusResult>();
            #endregion fields

            protected override void Setup()
            {
                Tick += OnTick;

                _setting = LoadSetting<InsaneStuntBonusSetting>();
                if (_setting == null || !_setting.Validate())
                {
                    _setting = new InsaneStuntBonusSetting();
                    SaveSetting(_setting);
                }
            }

            internal void OnTick(object o, EventArgs e)
            {
                var player = Game.Player.Character;
                if (!player.ExistsSafe())
                {
                    _isStunting = false;
                    return;
                }

                if (IsPerfectLandingDetectionEnabled)
                {
                    ProcessStuntResult(StuntResults);
                }

                var playerVeh = player.CurrentVehicle;

                if (playerVeh.ExistsSafe() && playerVeh.IsQualifiedForInsaneStunt() && (_currentVehicle == null || _currentVehicle != playerVeh))
                {
                    _currentVehicle = playerVeh;
                    _isStunting = false;
                }

                if (!_currentVehicle.ExistsSafe())
                {
                    _currentVehicle = null;
                    _isStunting = false;
                    return;
                }

                if (player.IsInVehicle(_currentVehicle) && _currentVehicle.IsAlive && _currentVehicle.IsInAir)
                {
                    if (!_isStunting)
                    {
                        InitInsaneStuntVars(_currentVehicle);
                    }

                    _currentVehiclePos = _currentVehicle.Position;

                    if (!_currentVehicle.IsUpsideDown && _wasFlippedInPrevFrame)
                    {
                        _wasFlippedInPrevFrame = false;
                    }
                    else if (_currentVehicle.IsUpsideDown && !_wasFlippedInPrevFrame)
                    {
                        _flipCount += 1;
                        _wasFlippedInPrevFrame = true;
                    }

                    if (_currentVehicle.Heading != _prevVehicleHeading)
                    {
                        var currentHeading = _currentVehicle.Heading;
                        var deltaHeading = Math.Abs(currentHeading - _prevVehicleHeading);
                        _prevVehicleHeading = currentHeading;

                        if (deltaHeading > 180f)
                        {
                            deltaHeading = 360f - deltaHeading;
                        }

                        _totalHeadingRotation += deltaHeading;
                    }
                    if (_currentVehiclePos.Z > _maxVehicleZPos)
                    {
                        _maxVehicleZPos = _currentVehiclePos.Z;
                    }
                }
                else if (_isStunting)
                {
                    _isStunting = false;

                    var endVehiclePos = _currentVehicle.Position;

                    var stuntHeight = _maxVehicleZPos - _initVehiclePos.Z;

                    var distance2d = Vector3.Distance2D(_initVehiclePos, endVehiclePos);

                    var stuntBonusMult = GetStuntBonusMult(distance2d, stuntHeight, _flipCount, _totalHeadingRotation);

                    if (stuntBonusMult > 0)
                    {
                        if (IsPerfectLandingDetectionEnabled)
                        {
                            StuntResults.Add(new InsaneStuntBonusResult(_currentVehicle, distance2d, stuntHeight, _flipCount, _totalHeadingRotation, (uint)Game.GameTime));
                        }
                        else
                        {
                            var bonusMoney = CalculateBonusMoney(distance2d, stuntHeight, _flipCount, _totalHeadingRotation, stuntBonusMult, false);
                            Game.Player.Money += (int)bonusMoney;
                            ShowInsaneStuntResult(distance2d, stuntHeight, _flipCount, _totalHeadingRotation, bonusMoney, stuntBonusMult, false);
                        }
                    }
                }
            }

            private void ProcessStuntResult(List<InsaneStuntBonusResult> results)
            {
                results.RemoveAll(x => !x.Vehicle.ExistsSafe() || x.Vehicle.IsDead);

                var player = Game.Player.Character;
                var gameTimeNow = (uint)Game.GameTime;

                for (int i = results.Count - 1; i >= 0; i--)
                {
                    if (!player.IsInVehicle(results[i].Vehicle) || (gameTimeNow - results[i].GameTimeOnFinish) > 3000)
                    {
                        var distance2d = results[i].DistanceXY;
                        var stuntHeight = results[i].StuntHeight;
                        var flipCount = results[i].FlipCount;
                        var totalHeadingRotation = results[i].TotalHeadingRotation;

                        var stuntBonusMult = GetStuntBonusMult(distance2d, stuntHeight, flipCount, totalHeadingRotation);

                        if (stuntBonusMult > 0)
                        {
                            var veh = results[i].Vehicle;
                            var bodyHealthDiff = results[i].VehicleBodyHealth - veh.BodyHealth;
                            var EngineHealthDiff = results[i].VehicleEngineHealth - veh.EngineHealth;
                            var FuelTankHealthDiff = results[i].VehicleFuelTankHealth - veh.PetrolTankHealth;

                            bool perfectLanding = ( player.IsInVehicle(veh)
                                                    && bodyHealthDiff < PerfectLandingHealthThreshold
                                                    && EngineHealthDiff < PerfectLandingHealthThreshold
                                                    && FuelTankHealthDiff < PerfectLandingHealthThreshold);

                            var bonusMoney = CalculateBonusMoney(distance2d, stuntHeight, flipCount, totalHeadingRotation, stuntBonusMult, perfectLanding);
                            Game.Player.Money += (int)bonusMoney;
                            ShowInsaneStuntResult(distance2d, stuntHeight, flipCount, totalHeadingRotation, bonusMoney, stuntBonusMult, perfectLanding);
                        }
                        results.RemoveAt(i);
                    }
                }
            }

            private uint CalculateBonusMoney(float distance2d, float stuntHeight, uint stuntFlipCount, float totalHeadingRotation, uint bonusMultiplier, bool perfectLanding)
            {
                var bonusMoney = (stuntFlipCount * 180) + ((uint)totalHeadingRotation) + ((uint)distance2d * 6) + ((uint)stuntHeight * 45);
                bonusMoney *= bonusMultiplier;
                if (perfectLanding)
                {
                    bonusMultiplier *= 2;
                }
                bonusMoney /= 15;

                return bonusMoney;
            }

            private void ShowInsaneStuntResult(float distance2d, float stuntHeight, uint stuntFlipCount, float totalHeadingRotation, uint bonusMoney, uint bonusMultiplier, bool perfectLanding)
            {
                var tupleStr = String.Empty;

                if (Game.Language == Language.Japanese)
                {
                    switch (bonusMultiplier)
                    {
                        case 1:
                            break;
                        case 2:
                            tupleStr = "ダブル・";
                            break;
                        case 3:
                            tupleStr = "トリプル・";
                            break;
                        case 4:
                            tupleStr = "カルテット・";
                            break;
                        default:
                            break;
                    }
    }
                else
                {
                    switch (bonusMultiplier)
                    {
                        case 1:
                            break;
                        case 2:
                            tupleStr = "DOUBLE ";
                            break;
                        case 3:
                            tupleStr = "TRIPLE ";
                            break;
                        case 4:
                            tupleStr = "QUADRUPLE ";
                            break;
                        default:
                            break;
                    }
                }

                var resultStyle = UseNotificationsToShowResult ? ShowingResultStyle.Notification : ShowingResultStyle.Subtitle;
                if (Game.Language == Language.Japanese)
                {
                    var perfectLandingStr1 = perfectLanding ? "パーフェクト・" : string.Empty;
                    var perfectLandingStr2 = perfectLanding ? "~n~おまけに完璧な着地だ！" : string.Empty;
                    ShowResult(String.Format("{0}{2}クレイジースタントボーナス！ {1}ドル", tupleStr, bonusMoney, perfectLandingStr1), resultStyle, 2000);
                    ShowResult(String.Format("距離: {0}m 高さ: {1}m 縦回転: {2} 横回転: {3}度{4}", distance2d, stuntHeight, stuntFlipCount, totalHeadingRotation, perfectLandingStr2), resultStyle, 5000);
                }
                else
                {
                    var perfectLandingStr1 = perfectLanding ? "PERFECT " : string.Empty;
                    var perfectLandingStr2 = perfectLanding ? "~n~And what a great landing!" : string.Empty;
                    ShowResult(String.Format("{2}{0}INSANE STUNT BONUS: ${1}", tupleStr, bonusMoney, perfectLandingStr1), resultStyle, 2000);
                    ShowResult(String.Format("Distance: {0}m Height: {1}m Flips: {2} Rotation: {3}°{4}", distance2d, stuntHeight, stuntFlipCount, totalHeadingRotation, perfectLandingStr2), resultStyle, 5000);
                }
            }

            private uint GetStuntBonusMult(float distance2d, float stuntHeight, uint stuntFlipCount, float totalHeadingRotation)
            {
                uint stuntBonusMult = 0;

                if (stuntHeight > 6.0f)
                {
                    stuntBonusMult++;
                }

                if (distance2d > 40.0f)
                {
                    stuntBonusMult++;
                }

                if (stuntFlipCount > 1)
                {
                    stuntBonusMult++;
                }

                if (totalHeadingRotation > 360f)
                {
                    stuntBonusMult++;
                }

                return stuntBonusMult;
            }

            private void InitInsaneStuntVars(Vehicle veh)
            {
                if (!veh.ExistsSafe())
                {
                    return;
                }

                _totalHeadingRotation = 0;
                _flipCount = 0;
                _wasFlippedInPrevFrame = false;
                _maxVehicleZPos = float.MinValue;
                _isStunting = true;

                _initVehiclePos = veh.Position;
                _prevVehicleHeading = veh.Heading;
            }
        }
    }
}
