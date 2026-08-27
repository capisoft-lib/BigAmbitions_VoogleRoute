using System;
using System.Collections.Generic;
using System.IO;
using BAModAPI;
using UnityEngine;
using VoogleRoute.Navigation;

namespace VoogleRoute
{
    /// <summary>Persists bookmarks, quick shortcuts, and visit history in the active save's modData.</summary>
    internal static class BookmarkDataSaveStore
    {
        private const string ModDataKey = "VoogleRoute.bookmarkData.v1";

        private static string _boundSaveId;
        private static bool _migratedGlobalFile;
        private static bool _loading;

        internal static void Initialize()
        {
            _migratedGlobalFile = false;
            RebindSaveIfNeeded(forceReload: true);
        }

        internal static void Tick() => RebindSaveIfNeeded(forceReload: false);

        internal static void Shutdown()
        {
            _boundSaveId = null;
            _migratedGlobalFile = false;
            _loading = false;
        }

        internal static void ReloadAllForCurrentSave()
        {
            _loading = true;
            try
            {
                var data = LoadForCurrentSave();
                BookmarkStore.LoadFromConfig(data.Bookmarks, persistChanges: false);
                QuickBookmarkStore.LoadFromConfig(data.QuickBookmarks);
                VisitHistoryStore.LoadFromConfig(data.VisitHistory, persistChanges: false);
            }
            finally
            {
                _loading = false;
            }
        }

        internal static void PersistCurrent()
        {
            if (_loading)
                return;

            if (string.IsNullOrEmpty(_boundSaveId))
                RebindSaveIfNeeded(forceReload: false);

            WriteToModData(CaptureCurrentData());
        }

        private static BookmarkFileData LoadForCurrentSave()
        {
            if (TryLoadFromModData(out var data))
            {
                UpgradeLegacyDataIfNeeded(data);
                return data;
            }

            if (TryMigrateFromBookmarkFile(out data))
            {
                UpgradeLegacyDataIfNeeded(data);
                WriteToModData(data);
                StripPerSaveDataFromBookmarkFile();
                return data;
            }

            return CreateEmptyData();
        }

        private static BookmarkFileData CaptureCurrentData() =>
            new BookmarkFileData
            {
                SchemaVersion = BookmarkFileData.CurrentSchemaVersion,
                Bookmarks = BookmarkStore.ExportToConfig(),
                VisitHistory = VisitHistoryStore.ExportToConfig(),
                QuickBookmarks = QuickBookmarkStore.ExportToConfig()
            };

        private static void RebindSaveIfNeeded(bool forceReload)
        {
            var saveId = ResolveSaveId();
            if (!forceReload && saveId == _boundSaveId)
                return;

            _boundSaveId = saveId;
            _migratedGlobalFile = false;
            ReloadAllForCurrentSave();
            ModOptionsSaveStore.ReloadForCurrentSave();
        }

        private static bool TryLoadFromModData(out BookmarkFileData data)
        {
            data = null;
            var save = SaveGameManager.Current;
            if (save?.modData == null)
                return false;

            if (save.modData.TryGetValue(ModDataKey, out var json) &&
                !string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    data = BookmarkJsonCodec.Deserialize(json);
                    EnsureDefaults(data);
                    return HasAnyData(data);
                }
                catch (Exception ex)
                {
                    ModLog.Error("[WARN] Failed to read bookmark data from save modData: " + ex.Message);
                    return false;
                }
            }

            return TryMigrateLegacyVisitHistoryModData(out data);
        }

        private static bool TryMigrateLegacyVisitHistoryModData(out BookmarkFileData data)
        {
            data = null;
            var save = SaveGameManager.Current;
            if (save?.modData == null)
                return false;

            const string legacyHistoryKey = "VoogleRoute.visitHistory.v1";
            if (!save.modData.TryGetValue(legacyHistoryKey, out var json) ||
                string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                data = CreateEmptyData();
                data.VisitHistory = BookmarkJsonCodec.DeserializeVisitHistory(json);
                save.modData.Remove(legacyHistoryKey);
                ModLog.Info("Migrated legacy visit-history modData into unified bookmark data.");
                return HasAnyData(data);
            }
            catch (Exception ex)
            {
                ModLog.Error("[WARN] Failed to migrate legacy visit-history modData: " + ex.Message);
                return false;
            }
        }

        private static bool TryMigrateFromBookmarkFile(out BookmarkFileData data)
        {
            data = null;
            if (_migratedGlobalFile)
                return false;

            _migratedGlobalFile = true;

            try
            {
                data = new BookmarkFileData
                {
                    SchemaVersion = BookmarkFileStore.SchemaVersion,
                    Bookmarks = CopyEntries(BookmarkFileStore.Bookmarks),
                    VisitHistory = CopyEntries(BookmarkFileStore.VisitHistory),
                    QuickBookmarks = CopyQuickBookmarks(BookmarkFileStore.QuickBookmarks)
                };

                if (!HasAnyData(data))
                    return false;

                ModLog.Info(
                    "Migrating bookmark data from bookmarks.json into save modData " +
                    "(bookmarks=" + data.Bookmarks.Count +
                    ", history=" + data.VisitHistory.Count + ").");
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Error("[WARN] Failed to migrate bookmark data from bookmarks.json: " + ex.Message);
                return false;
            }
        }

        private static void WriteToModData(BookmarkFileData data)
        {
            var save = SaveGameManager.Current;
            if (save == null || string.IsNullOrEmpty(_boundSaveId))
                return;

            try
            {
                EnsureDefaults(data);
                save.modData ??= new Dictionary<string, string>();
                save.modData[ModDataKey] = BookmarkJsonCodec.Serialize(data);
            }
            catch (Exception ex)
            {
                ModLog.Error("[WARN] Failed to write bookmark data to save modData: " + ex.Message);
            }
        }

        private static void StripPerSaveDataFromBookmarkFile()
        {
            try
            {
                BookmarkFileStore.SetBookmarks(new List<BookmarkConfigEntry>());
                BookmarkFileStore.SetVisitHistory(new List<BookmarkConfigEntry>());
                BookmarkFileStore.SetQuickBookmarks(new QuickBookmarksConfig());
            }
            catch (Exception ex)
            {
                ModLog.Error("[WARN] Failed to clear legacy bookmark data from bookmarks.json: " + ex.Message);
            }
        }

        private static BookmarkFileData CreateEmptyData() =>
            new BookmarkFileData
            {
                SchemaVersion = BookmarkFileData.CurrentSchemaVersion,
                Bookmarks = new List<BookmarkConfigEntry>(),
                VisitHistory = new List<BookmarkConfigEntry>(),
                QuickBookmarks = new QuickBookmarksConfig()
            };

        private static void EnsureDefaults(BookmarkFileData data)
        {
            if (data == null)
                return;

            if (data.Bookmarks == null)
                data.Bookmarks = new List<BookmarkConfigEntry>();

            if (data.VisitHistory == null)
                data.VisitHistory = new List<BookmarkConfigEntry>();

            if (data.QuickBookmarks == null)
                data.QuickBookmarks = new QuickBookmarksConfig();
        }

        private static void UpgradeLegacyDataIfNeeded(BookmarkFileData data)
        {
            if (!BookmarkDataMigration.TryUpgrade(data, out var removedBookmarks, out var removedHistory))
                return;

            WriteToModData(data);
            ModLog.Info(
                "Upgraded bookmark data schema: removed " + removedBookmarks +
                " History row(s) leaked into Bookmarks and " + removedHistory +
                " invalid quick row(s) leaked into History.");
        }

        private static bool HasAnyData(BookmarkFileData data)
        {
            if (data == null)
                return false;

            if (data.Bookmarks != null && data.Bookmarks.Count > 0)
                return true;

            if (data.VisitHistory != null && data.VisitHistory.Count > 0)
                return true;

            return HasQuickBookmarkData(data.QuickBookmarks);
        }

        private static bool HasQuickBookmarkData(QuickBookmarksConfig quick) =>
            quick != null &&
            (quick.LastCar != null || quick.LastHome != null || quick.LastShop != null);

        private static List<BookmarkConfigEntry> CopyEntries(IReadOnlyList<BookmarkConfigEntry> source)
        {
            var list = new List<BookmarkConfigEntry>();
            if (source == null)
                return list;

            for (var i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                    list.Add(source[i]);
            }

            return list;
        }

        private static QuickBookmarksConfig CopyQuickBookmarks(QuickBookmarksConfig source)
        {
            if (source == null)
                return new QuickBookmarksConfig();

            return new QuickBookmarksConfig
            {
                LastCar = source.LastCar,
                LastHome = source.LastHome,
                LastShop = source.LastShop
            };
        }

        private static string ResolveSaveId()
        {
            try
            {
                var save = SaveGameManager.Current;
                if (save == null)
                    return null;

                var characterId = save.characterId;
                var saveName = save.SaveGameName;
                if (string.IsNullOrWhiteSpace(characterId) && string.IsNullOrWhiteSpace(saveName))
                    return null;

                return Sanitize(characterId ?? "character") + "__" + Sanitize(saveName ?? "save");
            }
            catch
            {
                return null;
            }
        }

        private static string Sanitize(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value.Trim();
        }
    }

    internal static class BookmarkDataMigration
    {
        internal static bool TryUpgrade(
            BookmarkFileData data,
            out int removedBookmarks,
            out int removedHistory)
        {
            removedBookmarks = 0;
            removedHistory = 0;
            if (data == null || data.SchemaVersion >= BookmarkFileData.CurrentSchemaVersion)
                return false;

            data.Bookmarks ??= new List<BookmarkConfigEntry>();
            data.VisitHistory ??= new List<BookmarkConfigEntry>();
            data.QuickBookmarks ??= new QuickBookmarksConfig();

            for (var i = data.Bookmarks.Count - 1; i >= 0; i--)
            {
                var bookmark = data.Bookmarks[i];
                if (bookmark == null)
                {
                    data.Bookmarks.RemoveAt(i);
                    removedBookmarks++;
                    continue;
                }

                var isKnownParserLeak =
                    !bookmark.UserCreated &&
                    string.IsNullOrWhiteSpace(bookmark.Name) &&
                    MatchesAny(bookmark, data.VisitHistory);
                if (isKnownParserLeak)
                {
                    data.Bookmarks.RemoveAt(i);
                    removedBookmarks++;
                    continue;
                }

                // Before schema v2 the add dialog was the only legitimate writer.
                // Anything that does not match the known parser leak is preserved.
                bookmark.UserCreated = true;
            }

            for (var i = data.VisitHistory.Count - 1; i >= 0; i--)
            {
                var entry = data.VisitHistory[i];
                if (entry == null || entry.WorldOnly)
                {
                    data.VisitHistory.RemoveAt(i);
                    removedHistory++;
                    continue;
                }

                entry.UserCreated = false;
            }

            SetSystemOwned(data.QuickBookmarks.LastCar);
            SetSystemOwned(data.QuickBookmarks.LastHome);
            SetSystemOwned(data.QuickBookmarks.LastShop);
            data.SchemaVersion = BookmarkFileData.CurrentSchemaVersion;
            return true;
        }

        private static bool MatchesAny(
            BookmarkConfigEntry bookmark,
            IReadOnlyList<BookmarkConfigEntry> history)
        {
            if (bookmark == null || history == null)
                return false;

            for (var i = 0; i < history.Count; i++)
            {
                if (SamePlace(bookmark, history[i]))
                    return true;
            }

            return false;
        }

        private static bool SamePlace(BookmarkConfigEntry left, BookmarkConfigEntry right)
        {
            if (left == null || right == null)
                return false;

            var leftHasAddress = !string.IsNullOrWhiteSpace(left.StreetName) || left.StreetNumber > 0;
            var rightHasAddress = !string.IsNullOrWhiteSpace(right.StreetName) || right.StreetNumber > 0;
            if (leftHasAddress && rightHasAddress)
            {
                return left.StreetNumber == right.StreetNumber &&
                       string.Equals(left.StreetName, right.StreetName, StringComparison.OrdinalIgnoreCase);
            }

            var dx = left.WorldX - right.WorldX;
            var dy = left.WorldY - right.WorldY;
            var dz = left.WorldZ - right.WorldZ;
            var leftHasWorld = left.WorldX * left.WorldX + left.WorldY * left.WorldY + left.WorldZ * left.WorldZ > 0.01f;
            var rightHasWorld = right.WorldX * right.WorldX + right.WorldY * right.WorldY + right.WorldZ * right.WorldZ > 0.01f;
            return leftHasWorld && rightHasWorld && dx * dx + dy * dy + dz * dz < 4f;
        }

        private static void SetSystemOwned(BookmarkConfigEntry entry)
        {
            if (entry != null)
                entry.UserCreated = false;
        }
    }
}
