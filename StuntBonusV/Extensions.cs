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
        readonly static int _vehicleModelHasGliderOffset;
        readonly static int _vehicleGliderStateOffset;
        readonly static int _handlingDataOffsetOfCVehicle;
        readonly static int _hoverTransformRatioOffset;
        readonly static Dictionary<ulong, int> _subHandlingTypeCacheDict = new Dictionary<ulong, int>();
        readonly static GameVersion _gameVersion;

        private delegate ulong GetSubHandlingAddress(ulong handlingAddr, [MarshalAs(UnmanagedType.I4)]SubHandlingDataType subHandlingDataType);
        readonly static GetSubHandlingAddress GetSubHandlingAddressFunc;

        private enum VehicleGliderState
        {
            Retracted = 0,
            Deploying = 1,
            Deployed = 2,
            Retracting = 3
        }

        private enum SubHandlingDataType
        {
            CVehicleWeaponHandlingData = 9,
            CSpecialFlightHandlingData = 10,
        }

        static VehicleExtensions()
        {
            unsafe
            {
                var addr = MemoryAccess.FindPattern("\x48\x8B\x48\x20\x44\x8B\x81\x00\x00\x00\x00\x41\xC1\xE8\x09", "xxxxxxx????xxxx");
                if (addr != null)
                    _vehicleModelHasGliderOffset = *(int*)(addr + 7);
                addr = MemoryAccess.FindPattern("\x8B\x81\x00\x00\x00\x00\x48\x8B\xD9\x83\xF8\x02\x74\x20", "xx????xxxxxxxx");
                if (addr != null)
                    _vehicleGliderStateOffset = *(int*)(addr + 2);

                addr = MemoryAccess.FindPattern("\x74\x1E\x48\x8B\x88\x00\x00\x00\x00\xBA\x0A\x00\x00\x00\xE8", "xxxxx????xxxxxx");
                if (addr != null)
                {
                    _handlingDataOffsetOfCVehicle = *(int*)(addr + 0x5);
                    _hoverTransformRatioOffset = *(int*)(addr + 0x1C);
                    GetSubHandlingAddressFunc =  Marshal.GetDelegateForFunctionPointer<GetSubHandlingAddress>(new IntPtr((long)*(int*)(addr + 15) + addr + 19));
                }

                _gameVersion = Game.Version;
            }
        }

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

        internal static bool IsInSkiingStunt(this Vehicle vehicle, out bool isSkiingWithTwoWheels)
        {
            isSkiingWithTwoWheels = false;

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

            var isSkiing = (areAllLeftWheelsTouching && touchingRightWheelCount == 0) || (areAllRightWheelsTouching && touchingLeftWheelCount == 0);
            if (isSkiing && (touchingLeftWheelCount == 2 || touchingRightWheelCount == 2))
                isSkiingWithTwoWheels = true;

            return isSkiing;
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

        // for oppressor
        private static bool HasGlider(this Vehicle vehicle)
        {
            if (_vehicleModelHasGliderOffset == 0)
                return false;

            var address = vehicle.MemoryAddress;
            if (address == IntPtr.Zero)
                return false;

            unsafe
            {
                var modelInfo = (byte*)*(ulong*)(address + 0x20).ToPointer();
                return ((*(uint*)(modelInfo + _vehicleModelHasGliderOffset) >> 9) & 1) != 0;
            }
        }

        private static VehicleGliderState GetGliderState(this Vehicle vehicle)
        {
            if (!vehicle.HasGlider())
                return VehicleGliderState.Retracted;

            var address = vehicle.MemoryAddress;

            unsafe
            {
                return (VehicleGliderState)(*(int*)(address + _vehicleGliderStateOffset).ToPointer());
            }
        }

        private static float GetHoverTransformRatio(this Vehicle vehicle)
        {
            var address = vehicle.MemoryAddress;
            if (address == IntPtr.Zero)
                return 0f;

            unsafe
            {
                var handlingDataAddress = *(ulong*)(address + _handlingDataOffsetOfCVehicle).ToPointer();
                var subHandlingAddr = GetSubHandlingAddressFunc(handlingDataAddress, SubHandlingDataType.CSpecialFlightHandlingData);
                if (subHandlingAddr != 0)
                {
                    return *(float*)(address + _hoverTransformRatioOffset).ToPointer();
                }
            }

            return 0f;
        }

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

        static internal bool IsSpecialAbilityThatHelpFlyingUsing(this Vehicle vehicle)
        {
            if (vehicle.Model == VehicleHash.Oppressor2)
                return true;

            var gameVersion = _gameVersion;
            if (gameVersion < GameVersion.v1_0_944_2_Steam)
                return false;

            if (Function.Call<bool>(Hash._IS_VEHICLE_ROCKET_BOOST_ACTIVE, vehicle))
                return true;

            if (gameVersion < GameVersion.v1_0_1103_2_Steam)
                return false;

            var gliderState = vehicle.GetGliderState();
            if (gliderState == VehicleGliderState.Deploying || gliderState == VehicleGliderState.Deployed)
                return true;

            if (gameVersion < GameVersion.v1_0_1290_1_Steam)
                return false;

            var transformRatio = vehicle.GetHoverTransformRatio();
            if (transformRatio > 0f)
                return true;

            return false;
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
