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
            private bool _wasSpecialFeaturesThatHelpFlyingUsed = false;
            private bool _wasFlippedInPrevFrame = true;
            private bool _isStunting = false;
            private float _maxVehicleZPos = float.MinValue;
            private Vehicle _currentStuntingVehicle = null;
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
                if (!player.SafeExists())
                {
                    _isStunting = false;
                    return;
                }

                if (IsPerfectLandingDetectionEnabled)
                {
                    ProcessStuntResult(StuntResults);
                }

                var playerVeh = player.CurrentVehicle;
                var currentStuntingVehicle = _currentStuntingVehicle;

                if (playerVeh.SafeExists() && playerVeh != currentStuntingVehicle && playerVeh.IsQualifiedForInsaneStunt())
                {
                    currentStuntingVehicle = playerVeh;
                    _currentStuntingVehicle = playerVeh;
                    _isStunting = false;
                    _wasSpecialFeaturesThatHelpFlyingUsed = false;
                }

                if (!currentStuntingVehicle.SafeExists())
                {
                    _currentStuntingVehicle = null;
                    _wasSpecialFeaturesThatHelpFlyingUsed = false;
                    _isStunting = false;

                    return;
                }

                if (!_wasSpecialFeaturesThatHelpFlyingUsed && currentStuntingVehicle.IsSpecialAbilityThatHelpFlyingUsing())
                {
                    _wasSpecialFeaturesThatHelpFlyingUsed = true;
                    _isStunting = false;
                }

                if (_wasSpecialFeaturesThatHelpFlyingUsed)
                {
                    if (!currentStuntingVehicle.IsInAir)
                        _wasSpecialFeaturesThatHelpFlyingUsed = false;

                    return;
                }
                else if (currentStuntingVehicle.IsInAir)
                {
                    if (!_isStunting)
                    {
                        InitInsaneStuntVars(currentStuntingVehicle);
                    }

                    _currentVehiclePos = currentStuntingVehicle.Position;
                    var vehIsUpsideDown = currentStuntingVehicle.IsUpsideDown;

                    if (!vehIsUpsideDown && _wasFlippedInPrevFrame)
                    {
                        _wasFlippedInPrevFrame = false;
                    }
                    else if (vehIsUpsideDown && !_wasFlippedInPrevFrame)
                    {
                        _flipCount += 1;
                        _wasFlippedInPrevFrame = true;
                    }

                    var currentHeading = currentStuntingVehicle.Heading;
                    if (currentHeading != _prevVehicleHeading)
                    {
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

                    if (!player.IsInVehicle())
                    {
                        return;
                    }

                    var endVehiclePos = currentStuntingVehicle.Position;

                    var stuntHeight = _maxVehicleZPos - _initVehiclePos.Z;

                    var distance2d = Vector3.Distance2D(_initVehiclePos, endVehiclePos);

                    var stuntBonusMult = GetStuntBonusMult(distance2d, stuntHeight, _flipCount, _totalHeadingRotation);

                    if (stuntBonusMult > 0)
                    {
                        if (IsPerfectLandingDetectionEnabled)
                        {
                            StuntResults.Add(new InsaneStuntBonusResult(currentStuntingVehicle, distance2d, stuntHeight, _flipCount, (int)_totalHeadingRotation, (uint)Game.GameTime));
                        }
                        else
                        {
                            var bonusMoney = CalculateBonusMoney(distance2d, stuntHeight, _flipCount, _totalHeadingRotation, stuntBonusMult, false);
                            Game.Player.Money += (int)bonusMoney;
                            ShowInsaneStuntResult(distance2d, stuntHeight, _flipCount, (int)_totalHeadingRotation, bonusMoney, stuntBonusMult, false);
                        }
                    }
                }
            }

            private void ProcessStuntResult(List<InsaneStuntBonusResult> results)
            {
                results.RemoveAll(x => !x.Vehicle.SafeExists() || x.Vehicle.IsDead);

                var player = Game.Player.Character;
                var gameTimeNow = (uint)Game.GameTime;

                for (int i = results.Count - 1; i >= 0; i--)
                {
                    var result = results[i];
                    if (!player.IsInVehicle(result.Vehicle) || (gameTimeNow - result.GameTimeOnFinish) > 3000)
                    {
                        var distance2d = result.DistanceXY;
                        var stuntHeight = result.StuntHeight;
                        var flipCount = result.FlipCount;
                        var totalHeadingRotation = result.TotalHeadingRotation;

                        var stuntBonusMult = GetStuntBonusMult(distance2d, stuntHeight, flipCount, totalHeadingRotation);

                        if (stuntBonusMult > 0)
                        {
                            var veh = result.Vehicle;
                            var bodyHealthDiff = result.VehicleBodyHealth - veh.BodyHealth;
                            var EngineHealthDiff = result.VehicleEngineHealth - veh.EngineHealth;
                            var FuelTankHealthDiff = result.VehicleFuelTankHealth - veh.PetrolTankHealth;

                            bool perfectLanding = ( player.IsInVehicle(veh)
                                                    && bodyHealthDiff < PerfectLandingHealthThreshold
                                                    && EngineHealthDiff < PerfectLandingHealthThreshold
                                                    && FuelTankHealthDiff < PerfectLandingHealthThreshold);

                            var bonusMoney = CalculateBonusMoney(distance2d, stuntHeight, flipCount, totalHeadingRotation, stuntBonusMult, perfectLanding);
                            Game.Player.Money += (int)bonusMoney;
                            ShowInsaneStuntResult(distance2d, stuntHeight, flipCount, (int)totalHeadingRotation, bonusMoney, stuntBonusMult, perfectLanding);
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
                    bonusMoney *= 2;
                }
                bonusMoney /= 15;

                return bonusMoney;
            }

            private void ShowInsaneStuntResult(float distance2d, float stuntHeight, uint stuntFlipCount, int totalHeadingRotationInt, uint bonusMoney, uint bonusMultiplier, bool perfectLanding)
            {
                var tupleStr = string.Empty;

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
                    var perfectLandingStr1 = perfectLanding ? "パーフェクト" : string.Empty;
                    var perfectLandingStr2 = perfectLanding ? "~n~おまけに完璧な着地だ！" : string.Empty;
                    ShowResult($"{tupleStr}{perfectLandingStr1}クレイジースタントボーナス！ {bonusMoney}ドル", resultStyle, 2000);
                    if (Game.MeasurementSystem == MeasurementSystem.Metric)
                    {
                        ShowResult($"距離: {distance2d:F2}m 高さ: {stuntHeight:F2}m 縦回転: {stuntFlipCount} 横回転: {totalHeadingRotationInt}度{perfectLandingStr2}", resultStyle, 5000);
                    }
                    else
                    {
                        var distance2dFeetInt = (int)MathUtil.MeterToFeet(distance2d);
                        var stuntHeightFeetInt = (int)MathUtil.MeterToFeet(stuntHeight);
                        ShowResult($"距離: {distance2dFeetInt}フィート 高さ: {stuntHeightFeetInt}フィート 縦回転: {stuntFlipCount} 横回転: {totalHeadingRotationInt}度{perfectLandingStr2}", resultStyle, 5000);
                    }
                }
                else
                {
                    var perfectLandingStr1 = perfectLanding ? "PERFECT " : string.Empty;
                    var perfectLandingStr2 = perfectLanding ? "~n~And what a great landing!" : string.Empty;
                    ShowResult($"{tupleStr}{perfectLandingStr1}INSANE STUNT BONUS: ${bonusMoney}", resultStyle, 2000);
                    if (Game.MeasurementSystem == MeasurementSystem.Metric)
                    {
                        ShowResult($"Distance: {distance2d:F2}m Height: {stuntHeight:F2}m Flips: {stuntFlipCount} Rotation: {totalHeadingRotationInt}°{perfectLandingStr2}", resultStyle, 5000);
                    }
                    else
                    {
                        var distance2dFeetInt = (int)MathUtil.MeterToFeet(distance2d);
                        var stuntHeightFeetInt = (int)MathUtil.MeterToFeet(stuntHeight);
                        ShowResult($"Distance: {distance2dFeetInt}ft Height: {stuntHeightFeetInt}ft Flips: {stuntFlipCount} Rotation: {totalHeadingRotationInt}°{perfectLandingStr2}", resultStyle, 5000);
                    }
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
                if (!veh.SafeExists())
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
