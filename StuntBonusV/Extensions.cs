using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using GTA;
using GTA.Native;

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
            Function.Call(Hash._PLAY_AMBIENT_SPEECH1, ped.Handle, speechName, "SPEECH_PARAMS_STANDARD");
        }

        internal static void PlayAmbientScreamSpeech(this Ped ped, string speechName)
        {
            var voiceName = ped.Gender == Gender.Male ? "WAVELOAD_PAIN_MALE" : "WAVELOAD_PAIN_FEMALE";
            Function.Call(Hash._PLAY_AMBIENT_SPEECH_WITH_VOICE, ped.Handle, speechName, voiceName, "SPEECH_PARAMS_FORCE_SHOUTED", 0);
        }
    }

    internal static class VehicleExtensions
    {
        readonly static int _WheelPtrCollectionOffset;
        readonly static int _WheelCountOffset;

        static VehicleExtensions()
        {
            _WheelPtrCollectionOffset = GetWheelPtrCollectionOffset();
            _WheelCountOffset = _WheelPtrCollectionOffset + 0x8;

            int GetWheelPtrCollectionOffset()
            {
                var offset = (Game.Version >= GameVersion.VER_1_0_372_2_STEAM ? 0xAA0 : 0xA80);
                offset = (Game.Version >= GameVersion.VER_1_0_505_2_STEAM ? 0xA90 : offset);
                offset = (Game.Version >= GameVersion.VER_1_0_791_2_STEAM ? 0xAB0 : offset);
                offset = (Game.Version >= GameVersion.VER_1_0_877_1_STEAM ? 0xAE0 : offset);
                offset = (Game.Version >= GameVersion.VER_1_0_944_2_STEAM ? 0xB10 : offset);
                offset = (Game.Version >= GameVersion.VER_1_0_1103_2_STEAM ? 0xB20 : offset);
                offset = (Game.Version >= GameVersion.VER_1_0_1180_2_STEAM ? 0xB40 : offset);

                return offset;
            }
        }

        internal static int GetWheelCount(this Vehicle vehicle)
        {
            unsafe
            {
                var vehMemoryAddress = (byte*)vehicle.MemoryAddress;
                if (vehMemoryAddress == null)
                {
                    return 0;
                }

                return *(int*)(vehMemoryAddress + _WheelCountOffset);
            }
        }

        internal static IntPtr GetWheelAddress(this Vehicle vehicle, int wheelArrayindex)
        {
            unsafe
            {
                var vehMemoryAddress = (byte*)vehicle.MemoryAddress;
                if (vehMemoryAddress == null)
                {
                    return IntPtr.Zero;
                }

                var wheelCount = *(int*)(vehMemoryAddress + _WheelCountOffset);
                if (wheelArrayindex < 0 || wheelArrayindex > wheelCount - 1)
                {
                    throw new ArgumentOutOfRangeException("wheelArrayindex");
                }

                var wheelCollectionPtr = (long*)(*(ulong*)(vehMemoryAddress + _WheelPtrCollectionOffset));
                var wheelPtr = ((long*)(*(ulong*)(wheelCollectionPtr + wheelArrayindex)));
                return new IntPtr(wheelPtr);
            }
        }

        internal static IntPtr[] GetAllWheelAddresses(this Vehicle vehicle)
        {
            unsafe
            {
                var vehMemoryAddress = (byte*)vehicle.MemoryAddress;
                if (vehMemoryAddress == null)
                {
                    return new IntPtr[0];
                }

                var wheelCount = *(int*)(vehMemoryAddress + _WheelCountOffset);
                var wheelPtrArray = new IntPtr[wheelCount];
                var wheelCollectionPtr = (long*)(*(ulong*)(vehMemoryAddress + _WheelPtrCollectionOffset));

                for (var i = 0; i < wheelPtrArray.Length; i++)
                {
                    wheelPtrArray[i] = new IntPtr((long*)(*(ulong*)(wheelCollectionPtr + i)));
                }
                return wheelPtrArray;
            }
        }

        internal static bool IsInWheelie(this Vehicle vehicle)
        {
            var model = vehicle.Model;
            //Note: Model.IsBike returns true if the vehicle model is bicycle
            if ((!model.IsBike && !model.IsQuadbike) || vehicle.IsOnAllWheels)
            {
                return false;
            }

            var wheelPtrs = GetAllWheelAddresses(vehicle);
            var isAnyRearWheelsTouching = false;

            foreach (var wheelPtr in wheelPtrs)
            {
                if (!IsWheelTouching(wheelPtr))
                {
                    continue;
                }

                if (!IsFrontWheel(wheelPtr))
                {
                    isAnyRearWheelsTouching = true;
                }
                else
                {
                    return false;
                }
            }

            return isAnyRearWheelsTouching;
        }

        internal static bool IsInStoppie(this Vehicle vehicle)
        {
            var model = vehicle.Model;
            //Note: Model.IsBike returns true if the vehicle model is bicycle
            if ((!model.IsBike && !model.IsQuadbike) || vehicle.IsOnAllWheels)
            {
                return false;
            }

            var wheelPtrs = GetAllWheelAddresses(vehicle);
            var isAnyFrontWheelsTouching = false;

            foreach (var wheelPtr in wheelPtrs)
            {
                if (!IsWheelTouching(wheelPtr))
                {
                    continue;
                }

                if (IsFrontWheel(wheelPtr))
                {
                    isAnyFrontWheelsTouching = true;
                }
                else
                {
                    return false;
                }
            }

            return isAnyFrontWheelsTouching;
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

            var wheelPtrs = GetAllWheelAddresses(vehicle);
            var areAllLeftWheelsTouching = true;
            var areAllRightWheelsTouching = true;
            var touchingLeftWheelCount = LeftWheelCount;
            var touchingRightWheelCount = RightWheelCount;

            foreach (var wheelPtr in wheelPtrs)
            {
                if (!IsWheelTouching(wheelPtr))
                {
                    if (IsRightWheel(wheelPtr))
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

            return (areAllLeftWheelsTouching && touchingRightWheelCount <= 0) || (areAllRightWheelsTouching && touchingLeftWheelCount <= 0);
        }

        internal static void GetLeftAndRightWheelCount(this Vehicle vehicle, out int leftWheelCount, out int rightWheelCount)
        {
            leftWheelCount = 0;
            rightWheelCount = 0;

            foreach (var wheelPtr in vehicle.GetAllWheelAddresses())
            {
                if (IsRightWheel(wheelPtr))
                {
                    rightWheelCount++;
                }
                else
                {
                    leftWheelCount++;
                }
            }
        }

        internal static bool IsRightWheel(IntPtr wheelAddress)
        {
            if (wheelAddress == IntPtr.Zero)
            {
                return false;
            }

            unsafe
            {
                // doesn't work well with some bikes
                return (*((byte*)wheelAddress.ToPointer() + 0x23) & 0x80) == 0;
            }
        }

        internal static bool IsFrontWheel(IntPtr wheelAddress)
        {
            if (wheelAddress == IntPtr.Zero)
            {
                return false;
            }

            unsafe
            {
                // doesn't work well with some trailers
                return (*((byte*)wheelAddress.ToPointer() + 0x27) & 0x80) == 0;
            }
        }

        internal static bool IsWheelTouching(IntPtr wheelAddress)
        {
            if (wheelAddress == IntPtr.Zero)
            {
                return false;
            }

            unsafe
            {
                return (*((byte*)wheelAddress.ToPointer() + 0x1EC) & 0x1) != 0;
            }
        }

        static internal bool IsSubmarine(this Vehicle vehicle)
        {
            unsafe
            {
                var vehAddress = new IntPtr(vehicle.MemoryAddress);
                if (vehicle.SafeExists() && vehicle.MemoryAddress != null)
                {
                    var modelInfo = Marshal.ReadIntPtr(vehAddress, 0x20);
                    return Marshal.ReadInt32(vehAddress, 0x318) == 0xF;
                }
                else
                {
                    return false;
                }
            }

        }

        static internal bool IsQualifiedForInsaneStunt(this Vehicle vehicle)
        {
            if (vehicle.SafeExists())
            {
                var vehModel = vehicle.Model;
                return !(vehModel.IsHelicopter || vehModel.IsPlane || vehModel.IsTrain || vehicle.IsSubmarine());
            }
            else
            {
                return false;
            }
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
