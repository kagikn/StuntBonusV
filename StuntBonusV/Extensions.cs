using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using GTA;
using GTA.Native;
using System.Collections.ObjectModel;

namespace StuntBonusV
{
    internal static class Extensions
    {
        internal static bool SafeExists(this Entity entity)
        {
            return entity?.Exists() ?? false;
        }

        internal static void PlayAmbientSpeech(this Ped ped, string speechName)
        {
            Function.Call(Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE, ped.Handle, speechName, "SPEECH_PARAMS_STANDARD");
        }

        internal static void PlayAmbientScreamSpeech(this Ped ped, string speechName)
        {
            var voiceName = ped.Gender == Gender.Male ? "WAVELOAD_PAIN_MALE" : "WAVELOAD_PAIN_FEMALE";
            Function.Call(Hash.PLAY_PED_AMBIENT_SPEECH_WITH_VOICE_NATIVE, ped.Handle, speechName, voiceName, "SPEECH_PARAMS_FORCE_SHOUTED", 0);
        }
    }

    internal static class VehicleExtensions
    {
        readonly static ReadOnlyCollection<VehicleWheelBoneId> _frontWheels
            = Array.AsReadOnly(new VehicleWheelBoneId[] { VehicleWheelBoneId.WheelLeftFront, VehicleWheelBoneId.WheelRightFront });
        readonly static ReadOnlyCollection<VehicleWheelBoneId> _rearWheels
            = Array.AsReadOnly(new VehicleWheelBoneId[] { VehicleWheelBoneId.WheelLeftRear, VehicleWheelBoneId.WheelRightRear });
        readonly static ReadOnlyCollection<VehicleWheelBoneId> _leftWheels
            = Array.AsReadOnly(new VehicleWheelBoneId[] {
                VehicleWheelBoneId.WheelLeftFront,
                VehicleWheelBoneId.WheelLeftRear,
                VehicleWheelBoneId.WheelLeftMiddle1,
                VehicleWheelBoneId.WheelLeftMiddle2,
                VehicleWheelBoneId.WheelLeftMiddle3
                });
        readonly static ReadOnlyCollection<VehicleWheelBoneId> _rightWheels
            = Array.AsReadOnly(new VehicleWheelBoneId[] {
                VehicleWheelBoneId.WheelRightFront,
                VehicleWheelBoneId.WheelRightRear,
                VehicleWheelBoneId.WheelRightMiddle1,
                VehicleWheelBoneId.WheelRightMiddle2,
                VehicleWheelBoneId.WheelRightMiddle3
                });

        internal static bool IsInWheelie(this Vehicle vehicle)
        {
            if ((!vehicle.IsBike && !vehicle.IsQuadBike))
            {
                return false;
            }

            var areAnyRearWheelsTouching = false;
            var areAnyNonRearWheelsTouching = false;

            foreach (var wheel in vehicle.Wheels)
            {
                if (!wheel.IsTouchingSurface)
                {
                    continue;
                }

                if (IsRearWheel(wheel))
                {
                    areAnyRearWheelsTouching = true;
                }
                else
                {
                    areAnyNonRearWheelsTouching = true;
                }
            }

            return areAnyRearWheelsTouching && !areAnyNonRearWheelsTouching;
        }

        internal static bool IsInStoppie(this Vehicle vehicle)
        {
            if ((!vehicle.IsBike && !vehicle.IsQuadBike))
            {
                return false;
            }

            var areAnyFrontWheelsTouching = false;
            var areAnyNonFrontWheelsTouching = false;

            foreach (var wheel in vehicle.Wheels)
            {
                if (!wheel.IsTouchingSurface)
                {
                    continue;
                }

                if (IsFrontWheel(wheel))
                {
                    areAnyFrontWheelsTouching = true;
                }
                else
                {
                    areAnyNonFrontWheelsTouching = true;
                }
            }

            return areAnyFrontWheelsTouching && !areAnyNonFrontWheelsTouching;
        }

        internal static bool IsInSkiingStunt(this Vehicle vehicle)
        {
            if (vehicle.IsOnAllWheels)
            {
                return false;
            }

            int LeftWheelCount, RightWheelCount;
            vehicle.GetLeftAndRightWheelCount(out LeftWheelCount, out RightWheelCount);
            if (LeftWheelCount == 0 || RightWheelCount == 0 || LeftWheelCount + RightWheelCount <= 2)
            {
                return false;
            }

            var areAllLeftWheelsTouching = true;
            var areAllRightWheelsTouching = true;
            var touchingLeftWheelCount = LeftWheelCount;
            var touchingRightWheelCount = RightWheelCount;

            foreach (var wheel in vehicle.Wheels)
            {
                if (wheel.IsTouchingSurface)
                {
                    if (IsRightWheel(wheel))
                    {
                        areAllRightWheelsTouching = false;
                        touchingRightWheelCount--;
                    }
                    else
                    {
                        areAllLeftWheelsTouching = false;
                        touchingLeftWheelCount--;
                    }

                    if (!areAllRightWheelsTouching && !areAllLeftWheelsTouching)
                    {
                        return false;
                    }
                }
            }

            return (areAllLeftWheelsTouching && touchingRightWheelCount == 0) || (areAllRightWheelsTouching && touchingLeftWheelCount == 0);
        }

        internal static void GetLeftAndRightWheelCount(this Vehicle vehicle, out int leftWheelCount, out int rightWheelCount)
        {
            leftWheelCount = 0;
            rightWheelCount = 0;

            foreach (var wheel in vehicle.Wheels)
            {
                if (IsRightWheel(wheel))
                {
                    rightWheelCount++;
                }
                else
                {
                    leftWheelCount++;
                }
            }
        }

        internal static bool IsLeftWheel(VehicleWheel wheel) => _leftWheels.Contains(wheel.BoneId);
        internal static bool IsRightWheel(VehicleWheel wheel) => _rightWheels.Contains(wheel.BoneId);
        internal static bool IsFrontWheel(VehicleWheel wheel) => _frontWheels.Contains(wheel.BoneId);
        internal static bool IsRearWheel(VehicleWheel wheel) => _rearWheels.Contains(wheel.BoneId);

        static internal bool IsSubmarine(this Vehicle vehicle) => vehicle.IsSubmarine;

        static internal bool IsQualifiedForInsaneStunt(this Vehicle vehicle)
        {
            var vehType = vehicle.Type;
            // boats, trains, and submarine cannot have wheels. Exclude VehicleType.None too, which will be returned if the vehicle does not exist from SHVDN
            if ((uint)vehType > 12)
                return false;
            // return false if the vehicle type is plane, helicopter, blimp, or autogyro
            if (vehType == VehicleType.Plane || ((uint)vehType - 8) <= 2)
                return false;

            return true;
        }
    }

    internal static class LinqExtensions
    {
        static Random _Rand = new Random();

        internal static T RandomAt<T>(this IEnumerable<T> ie)
        {
            if (ie.Any() == false) return default(T);
            return ie.ElementAt(_Rand.Next(0, ie.Count()));
        }
    }
}
