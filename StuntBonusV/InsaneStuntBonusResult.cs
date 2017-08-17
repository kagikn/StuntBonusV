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
        private Vehicle Vehicle { get; }
        private float DistanceXY { get; }
        private float StuntHeight { get; }
        private int FlipCount { get; }
        private float TotalHeadingRotation { get; }
        private float VehicleBodyHealth { get; }
        private float VehicleEngineHealth { get; }
        private float VehicleFuelTankHealth { get; }

        public InsaneStuntBonusResult(Vehicle vehicle, float distanceXY, float stuntHeight, int flipCount, int totalHeadingRotation)
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

            VehicleBodyHealth = vehicle.BodyHealth;
            VehicleEngineHealth = vehicle.EngineHealth;
            VehicleFuelTankHealth = vehicle.PetrolTankHealth;
        }
    }
}
