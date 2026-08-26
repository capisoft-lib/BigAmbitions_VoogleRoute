using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal readonly struct IndoorExitTarget
    {
        internal static readonly IndoorExitTarget None = default;

        internal IndoorExitTarget(
            Vector3 walkPosition,
            int exitZoneId,
            bool isCasinoExit,
            bool isParkingExit,
            bool isHamptonsPlotExit = false,
            Vector3 hamptonsOutsidePosition = default)
        {
            WalkPosition = walkPosition;
            ExitZoneId = exitZoneId;
            IsCasinoExit = isCasinoExit;
            IsParkingExit = isParkingExit;
            IsHamptonsPlotExit = isHamptonsPlotExit;
            HamptonsOutsidePosition = hamptonsOutsidePosition;
        }

        internal Vector3 WalkPosition { get; }

        internal int ExitZoneId { get; }

        internal bool IsCasinoExit { get; }

        internal bool IsParkingExit { get; }

        internal bool IsHamptonsPlotExit { get; }

        internal Vector3 HamptonsOutsidePosition { get; }

        internal bool IsValid => WalkPosition.sqrMagnitude > 0.01f;
    }
}
