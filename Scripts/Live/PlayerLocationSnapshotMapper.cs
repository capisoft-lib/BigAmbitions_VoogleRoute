using System.Globalization;
using BaPlayerLocation.Subscriber;
using UnityEngine;
using VoogleRoute.Navigation;

namespace VoogleRoute.Live
{
    internal enum PlayerLocationState
    {
        Unavailable,
        Inside,
        Subway,
        Car,
        Player
    }

    internal static class PlayerLocationSnapshotMapper
    {
        internal static PlayerLocationState ToLegacyState(MovementKind kind)
        {
            switch (kind)
            {
                case MovementKind.Indoor: return PlayerLocationState.Inside;
                case MovementKind.Walk: return PlayerLocationState.Player;
                case MovementKind.Car: return PlayerLocationState.Car;
                case MovementKind.Subway: return PlayerLocationState.Subway;
                default: return PlayerLocationState.Unavailable;
            }
        }

        internal static MovementMode ToMovementMode(MovementKind kind)
        {
            switch (kind)
            {
                case MovementKind.Walk:
                case MovementKind.Indoor:
                    return MovementMode.OnFoot;
                case MovementKind.Car:
                    return MovementMode.Vehicle;
                case MovementKind.Subway:
                    return MovementMode.Subway;
                default:
                    return MovementMode.Unavailable;
            }
        }

        internal static string FormatState(PlayerLocationState state)
        {
            switch (state)
            {
                case PlayerLocationState.Inside: return "Inside";
                case PlayerLocationState.Subway: return "Subway";
                case PlayerLocationState.Car: return "Car";
                case PlayerLocationState.Player: return "Player";
                default: return "Unavailable";
            }
        }

        internal static string FormatPosition(Vector3 position) =>
            "X=" + position.x.ToString("0.##", CultureInfo.InvariantCulture) +
            " Y=" + position.y.ToString("0.##", CultureInfo.InvariantCulture) +
            " Z=" + position.z.ToString("0.##", CultureInfo.InvariantCulture);

        internal static string FormatHeading(float headingDeg) =>
            headingDeg.ToString("0.#", CultureInfo.InvariantCulture) + "deg";

        internal static bool TryGetForward(MovementKind kind, float headingDeg, out Vector3 forward)
        {
            forward = Vector3.forward;
            if (kind != MovementKind.Car && kind != MovementKind.Walk && kind != MovementKind.Indoor)
                return false;

            var rad = headingDeg * Mathf.Deg2Rad;
            forward = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            return forward.sqrMagnitude > 0.01f;
        }
    }
}
