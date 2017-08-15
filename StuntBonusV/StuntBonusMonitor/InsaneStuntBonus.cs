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
        internal static class InsaneStuntBonus
        {
            static float _stuntTotalRotation = 0;
            static float _prevVehicleHeading = 0;
            static uint _stuntFlipCount = 0;
            static bool _wasFlippedInPrevFrame = true;
            static bool _isStunting = false;
            static float _maxVehicleZPos = float.MinValue;
            static Vehicle _currentVehicle = null;
            static Vector3 _currentVehiclePos = Vector3.Zero;
            static Vector3 _initVehiclePos = Vector3.Zero;

            internal static void OnTick(object o, EventArgs e)
            {

                var player = Game.Player.Character;
                if (!player.ExistsSafe())
                {
                    _isStunting = false;
                    return;
                }

                var playerVeh = player.CurrentVehicle;

                if (playerVeh.ExistsSafe() && _currentVehicle != playerVeh)
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

                if (_currentVehicle.IsQualifiedForInsaneStunt())
                {
                    _isStunting = false;
                    return;
                }

                if (_currentVehicle.IsAlive && playerVeh.IsInAir)
                {
                    if (!_isStunting)
                    {
                        InitInsaneStuntVars(playerVeh);
                    }

                    _currentVehiclePos = playerVeh.Position;

                    if (!playerVeh.IsUpsideDown && _wasFlippedInPrevFrame)
                    {
                        _wasFlippedInPrevFrame = false;
                    }
                    else if (playerVeh.IsUpsideDown && !_wasFlippedInPrevFrame)
                    {
                        _stuntFlipCount += 1;
                        _wasFlippedInPrevFrame = true;
                    }

                    if (playerVeh.Heading != _prevVehicleHeading)
                    {
                        var currentHeading = playerVeh.Heading;
                        var deltaHeading = Math.Abs(currentHeading - _prevVehicleHeading);
                        _prevVehicleHeading = currentHeading;

                        if (deltaHeading > 180f)
                        {
                            deltaHeading = 360f - deltaHeading;
                        }

                        _stuntTotalRotation += deltaHeading;
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

                    var stuntBonusMult = GetStuntBonusMult(stuntHeight, distance2d, _stuntFlipCount, _stuntTotalRotation);

                    if (stuntBonusMult > 0)
                    {
                        var bonusMoney = (_stuntFlipCount * 180) + ((uint)_stuntTotalRotation) + ((uint)distance2d * 6) + ((uint)stuntHeight * 45);
                        bonusMoney *= stuntBonusMult;
                        bonusMoney /= 15;

                        Game.Player.Money += (int)bonusMoney;

                        var tupleStr = String.Empty;

                        if (Game.Language == Language.Japanese)
                        {
                            switch (stuntBonusMult)
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
                            switch (stuntBonusMult)
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
                            GtaNativeUtil.ShowSubtitle(String.Format("{0}クレイジースタントボーナス！ {1}ドル", tupleStr, bonusMoney), 2000, false);
                            GtaNativeUtil.ShowSubtitle(String.Format("距離: {0}m 高さ: {1}m 縦回転: {2} 横回転: {3}度", distance2d, stuntHeight, _stuntFlipCount, _stuntTotalRotation), 5000, false);
                        }
                        else
                        {
                            GtaNativeUtil.ShowSubtitle(String.Format("{0}INSANE STUNT BONUS: ${1}", tupleStr, bonusMoney), 2000, false);
                            GtaNativeUtil.ShowSubtitle(String.Format("Distance: {0}m Height: {1}m Flips: {2} Rotation: {3}°", distance2d, stuntHeight, _stuntFlipCount, _stuntTotalRotation), 5000, false);
                        }
                    }
                }
            }



            static private uint GetStuntBonusMult(float stuntHeight, float distance2d, uint stuntFlipCount, float stuntRotation)
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

                if (_stuntFlipCount > 2)
                {
                    stuntBonusMult++;
                }

                if (_stuntTotalRotation > 360f)
                {
                    stuntBonusMult++;
                }

                return stuntBonusMult;
            }

            static private void InitInsaneStuntVars(Vehicle veh)
            {
                _stuntTotalRotation = 0;
                _stuntFlipCount = 0;
                _wasFlippedInPrevFrame = false;
                _maxVehicleZPos = float.MinValue;
                _isStunting = true;

                _currentVehicle = veh;
                _initVehiclePos = veh.Position;
                _prevVehicleHeading = veh.Heading;
            }
        }
    }
}
