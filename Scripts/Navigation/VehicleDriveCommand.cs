namespace VoogleRoute.Navigation
{
    internal readonly struct VehicleDriveCommand
    {
        internal float Throttle { get; }
        internal float Brakes { get; }
        internal float Steering { get; }

        internal VehicleDriveCommand(float throttle, float brakes, float steering)
        {
            Throttle = throttle;
            Brakes = brakes;
            Steering = steering;
        }

        internal static VehicleDriveCommand Stop => new VehicleDriveCommand(0f, 1f, 0f);
    }
}
