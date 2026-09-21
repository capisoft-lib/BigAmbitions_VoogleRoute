using System;
using Helpers;

namespace VoogleRoute.Navigation
{
    [Flags]
    internal enum BookmarkCategory
    {
        None = 0,
        Vehicles = 1,
        Residential = 2,
        Retail = 4,
        Office = 8,
        Other = 16,
        All = Vehicles | Residential | Retail | Office | Other
    }

    internal static class BookmarkCategoryResolver
    {
        internal static BookmarkCategory Resolve(BookmarkEntry entry)
        {
            if (entry == null || !entry.HasAddress)
                return BookmarkCategory.Other;
            try
            {
                var registration = BuildingHelper.GetBuildingRegistration(entry.ToAddress());
                return FromBuildingType(registration?.GetBuildingType());
            }
            catch
            {
                return BookmarkCategory.Other;
            }
        }

        internal static BookmarkCategory FromBuildingType(string type) => type switch
        {
            "ba:buildingtype_residential" => BookmarkCategory.Residential,
            "ba:buildingtype_retail" => BookmarkCategory.Retail,
            "ba:buildingtype_office" => BookmarkCategory.Office,
            _ => BookmarkCategory.Other
        };
    }
}
