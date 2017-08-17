using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;

namespace StuntBonusV
{
    internal class InsaneStuntBonusResult
    {
        public Vehicle Vehicle { get; }
        public float DistanceXY { get; }
        public float StuntHeight { get; }
        public uint FlipCount { get; }
        public float TotalHeadingRotation { get; }
        public float VehicleBodyHealth { get; }
        public float VehicleEngineHealth { get; }
        public float VehicleFuelTankHealth { get; }
        public uint GameTimeOnFinish { get; }

        public InsaneStuntBonusResult(Vehicle vehicle, float distanceXY, float stuntHeight, uint flipCount, float totalHeadingRotation, uint gameTime)
        {
            if (!vehicle.ExistsSafe())
            {
                throw new ArgumentNullException("vehicle");
            }

            Vehicle = vehicle;
            DistanceXY = distanceXY;
            StuntHeight = stuntHeight;
            FlipCount = flipCount;
            TotalHeadingRotation = totalHeadingRotation;
            GameTimeOnFinish = gameTime;

            VehicleBodyHealth = vehicle.BodyHealth;
            VehicleEngineHealth = vehicle.EngineHealth;
            VehicleFuelTankHealth = vehicle.PetrolTankHealth;
        }
    }
}
