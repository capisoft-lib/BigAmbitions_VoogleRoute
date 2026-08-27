using Streets;
using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal sealed class BookmarkEntry
    {
        internal string Name = "";
        internal string StreetName = "";
        internal int StreetNumber;
        internal float WorldX;
        internal float WorldY;
        internal float WorldZ;
        internal string LocationLabel = "";
        internal bool WorldOnly;
        internal bool UserCreated;

        internal bool HasWorldPosition => WorldPosition.sqrMagnitude > 0.01f;

        internal bool PrefersWorldPosition => WorldOnly || (!HasAddress && HasWorldPosition);

        internal Vector3 WorldPosition => new Vector3(WorldX, WorldY, WorldZ);

        internal Address ToAddress()
        {
            if (!HasAddress)
                return null;

            return new Address(StreetName, StreetNumber);
        }

        internal bool HasAddress =>
            !string.IsNullOrWhiteSpace(StreetName) || StreetNumber > 0;

        internal bool TryGetNavigationTarget(out Vector3 target)
        {
            target = default;
            if (PrefersWorldPosition && HasWorldPosition)
            {
                target = WorldPosition;
                return true;
            }

            if (HasAddress && DestinationResolver.TryResolveWorldPosition(ToAddress(), out target))
                return true;

            if (HasWorldPosition)
            {
                target = WorldPosition;
                return true;
            }

            return false;
        }

        internal string DisplayName =>
            string.IsNullOrWhiteSpace(Name) ? LocationLabel : Name;

        internal bool MatchesFilter(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            var f = filter.Trim();
            if (DisplayName.IndexOf(f, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (LocationLabel.IndexOf(f, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }

        internal bool SamePlaceAs(BookmarkEntry other)
        {
            if (other == null)
                return false;

            if (HasAddress && other.HasAddress)
                return StreetNumber == other.StreetNumber &&
                       string.Equals(StreetName, other.StreetName, System.StringComparison.OrdinalIgnoreCase);

            if (HasWorldPosition && other.HasWorldPosition)
                return (WorldPosition - other.WorldPosition).sqrMagnitude < 4f;

            if (HasAddress && other.HasWorldPosition &&
                TryResolveAddressWorld(out var myWorld) &&
                (myWorld - other.WorldPosition).sqrMagnitude < 4f)
                return true;

            if (other.HasAddress && HasWorldPosition &&
                other.TryResolveAddressWorld(out var otherWorld) &&
                (WorldPosition - otherWorld).sqrMagnitude < 4f)
                return true;

            return false;
        }

        private bool TryResolveAddressWorld(out Vector3 worldPos)
        {
            worldPos = default;
            if (!HasAddress)
                return false;

            return DestinationResolver.TryResolveWorldPosition(ToAddress(), out worldPos);
        }
    }
}
