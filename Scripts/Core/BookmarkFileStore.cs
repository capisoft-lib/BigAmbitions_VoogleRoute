using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using VoogleRoute.Navigation;

namespace VoogleRoute
{
  internal static class BookmarkFileStore
  {
    private static BookmarkFileData _data = CreateDefaultData();

    internal static string FilePath =>
      ModStoragePaths.FileInModRoot(ModStoragePaths.BookmarksFileName);

    internal static IReadOnlyList<BookmarkConfigEntry> Bookmarks => _data.Bookmarks;

    internal static IReadOnlyList<BookmarkConfigEntry> VisitHistory => _data.VisitHistory;

    internal static QuickBookmarksConfig QuickBookmarks => _data.QuickBookmarks;

    internal static void Load(ModConfigData legacyConfig = null)
    {
      _data = CreateDefaultData();

      try
      {
        var path = FilePath;
        if (File.Exists(path))
        {
          var json = File.ReadAllText(path, Encoding.UTF8);
          _data = BookmarkJsonCodec.Deserialize(json);
        }
        else if (TryMigrateFromLegacyConfig(legacyConfig))
        {
          Write();
          ModConfigStore.StripBookmarkDataAndSave();
          ModLog.Info("Migrated bookmarks from config.json to " + path);
        }
      }
      catch (Exception ex)
      {
        _data = CreateDefaultData();
        Debug.LogWarning("[VoogleRoute] Failed to read bookmarks.json: " + ex.Message);
      }

      EnsureDefaults(_data);
    }

    internal static void SetBookmarks(List<BookmarkConfigEntry> bookmarks)
    {
      _data.Bookmarks = bookmarks ?? new List<BookmarkConfigEntry>();
      Write();
    }

    internal static void SetQuickBookmarks(QuickBookmarksConfig quickBookmarks)
    {
      _data.QuickBookmarks = quickBookmarks ?? new QuickBookmarksConfig();
      Write();
    }

    internal static void SetVisitHistory(List<BookmarkConfigEntry> visitHistory)
    {
      _data.VisitHistory = visitHistory ?? new List<BookmarkConfigEntry>();
      Write();
    }

    private static BookmarkFileData CreateDefaultData() =>
      new BookmarkFileData
      {
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

    private static bool TryMigrateFromLegacyConfig(ModConfigData legacyConfig)
    {
      if (legacyConfig == null)
        return false;

      var hasBookmarks = legacyConfig.Bookmarks != null && legacyConfig.Bookmarks.Count > 0;
      var hasQuick = HasQuickBookmarkData(legacyConfig.QuickBookmarks);
      if (!hasBookmarks && !hasQuick)
        return false;

      if (hasBookmarks)
        _data.Bookmarks = new List<BookmarkConfigEntry>(legacyConfig.Bookmarks);

      if (hasQuick)
        _data.QuickBookmarks = legacyConfig.QuickBookmarks;

      return true;
    }

    private static bool HasQuickBookmarkData(QuickBookmarksConfig quick) =>
      quick != null &&
      (quick.LastCar != null || quick.LastHome != null || quick.LastShop != null);

    private static void Write()
    {
      try
      {
        EnsureDefaults(_data);
        var path = FilePath;
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
          Directory.CreateDirectory(directory);

        var json = BookmarkJsonCodec.Serialize(_data);
        File.WriteAllText(path, json, Encoding.UTF8);
      }
      catch (Exception ex)
      {
        Debug.LogWarning("[VoogleRoute] Failed to write bookmarks.json: " + ex.Message);
      }
    }
  }

  internal sealed class BookmarkFileData
  {
    public List<BookmarkConfigEntry> Bookmarks { get; set; }

    public List<BookmarkConfigEntry> VisitHistory { get; set; }

    public QuickBookmarksConfig QuickBookmarks { get; set; }
  }
}
