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
        internal class InsaneStuntBonus : StuntBonusScript
        {
            #region setting

            class InsaneStuntBonusSetting : Setting
            {
                public bool IsPerfectLandingDetectionEnabled { get; set; } = true;
                public bool UseNotificationsToShowResult { get; set; } = false;
                public override bool Validate()
                {
                    return true;
                }
            }
            public override string SettingFileName { get; } = "InsaneStuntBonus.xml";

            #endregion

            #region fields
            private InsaneStuntBonusSetting _setting;

            float _totalHeadingRotation = 0;
            float _prevVehicleHeading = 0;
            uint _flipCount = 0;
            bool _wasFlippedInPrevFrame = true;
            bool _isStunting = false;
            float _maxVehicleZPos = float.MinValue;
            Vehicle _currentVehicle = null;
            Vector3 _currentVehiclePos = Vector3.Zero;
            Vector3 _initVehiclePos = Vector3.Zero;

            bool IsPerfectLandingDetectionEnabled => _setting.IsPerfectLandingDetectionEnabled;
            bool UseNotificationsToShowResult => _setting.UseNotificationsToShowResult;

            List<InsaneStuntBonusResult> StuntResults = new List<InsaneStuntBonusResult>();
            #endregion fields

            protected override void Setup()
            {
                Tick += OnTick;

                var _setting = LoadSetting<InsaneStuntBonusSetting>();
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
                            var bonusMoney = CalculateBonusMoney(distance2d, stuntHeight, _flipCount, _totalHeadingRotation, stuntBonusMult);
                            Game.Player.Money += (int)bonusMoney;
                            ShowInsaneStuntResult(distance2d, stuntHeight, _flipCount, _totalHeadingRotation, bonusMoney, stuntBonusMult);
                        }
                    }
                }
            }

            private void ProcessStuntResult(List<InsaneStuntBonusResult> results)
            {
                results.RemoveAll(x => !x.Vehicle.ExistsSafe() && x.Vehicle.IsDead);

                var player = Game.Player.Character;
                var gameTimeNow = Game.GameTime;

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
                            var bonusMoney = CalculateBonusMoney(distance2d, stuntHeight, flipCount, totalHeadingRotation, stuntBonusMult);
                            Game.Player.Money += (int)bonusMoney;
                            ShowInsaneStuntResult(distance2d, stuntHeight, flipCount, totalHeadingRotation, bonusMoney, stuntBonusMult);
                        }
                    }

                    results.RemoveAt(i);
                }
            }

            private uint CalculateBonusMoney(float distance2d, float stuntHeight, uint stuntFlipCount, float totalHeadingRotation, uint bonusMultiplier)
            {
                var bonusMoney = (stuntFlipCount * 180) + ((uint)totalHeadingRotation) + ((uint)distance2d * 6) + ((uint)stuntHeight * 45);
                bonusMoney *= bonusMultiplier;
                bonusMoney /= 15;

                return bonusMoney;
            }

            private void ShowInsaneStuntResult(float distance2d, float stuntHeight, uint stuntFlipCount, float totalHeadingRotation, uint bonusMoney, uint bonusMultiplier)
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

                if (Game.Language == Language.Japanese)
                {
                    var resultStyle = UseNotificationsToShowResult ? ShowingResultStyle.Notification : ShowingResultStyle.Subtitle;
                    ShowResult(String.Format("{0}クレイジースタントボーナス！ {1}ドル", tupleStr, bonusMoney), resultStyle, 2000);
                    ShowResult(String.Format("距離: {0}m 高さ: {1}m 縦回転: {2} 横回転: {3}度", distance2d, stuntHeight, stuntFlipCount, totalHeadingRotation), resultStyle, 5000);
                }
                else
                {
                    var resultStyle = UseNotificationsToShowResult ? ShowingResultStyle.Notification : ShowingResultStyle.Subtitle;
                    ShowResult(String.Format("{0}INSANE STUNT BONUS: ${1}", tupleStr, bonusMoney), resultStyle, 2000);
                    ShowResult(String.Format("Distance: {0}m Height: {1}m Flips: {2} Rotation: {3}°", distance2d, stuntHeight, stuntFlipCount, totalHeadingRotation), resultStyle, 5000);
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
