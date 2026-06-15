using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoogleRoute.UI;

namespace VoogleRoute
{
    internal static class VisualTestScenarioRunner
    {
        internal static IEnumerator RunPostLoadSteps(IReadOnlyList<string> steps)
        {
            if (steps == null || steps.Count == 0)
                yield break;

            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i]?.Trim();
                if (string.IsNullOrEmpty(step))
                    continue;

                yield return RunStep(step);
            }

            Canvas.ForceUpdateCanvases();
            yield return null;
        }

        private static IEnumerator RunStep(string step)
        {
            switch (step.ToLowerInvariant())
            {
                case "closeallmodals":
                    RouteSettingsUi.Close();
                    CityMapBookmarkAddDialog.Close();
                    VisitHistoryPanel.Close();
                    AutoDriveConfirmPopup.Close();
                    break;

                case "refreshactionpanel":
                    RouteToggleHud.UpdateVisibility();
                    RouteToggleHud.RefreshVisual();
                    break;

                case "openvisithistory":
                    VisitHistoryPanel.Open();
                    break;

                case "closevisithistory":
                    VisitHistoryPanel.Close();
                    break;

                case "opensettings":
                    RouteSettingsUi.Open();
                    break;

                case "closesettings":
                    RouteSettingsUi.Close();
                    break;

                case "wait1":
                    yield return null;
                    yield break;

                case "wait2":
                    yield return null;
                    yield return null;
                    yield break;

                default:
                    if (step.StartsWith("wait", StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(step.Substring(4), out var frameCount) &&
                        frameCount > 0)
                    {
                        for (var f = 0; f < frameCount; f++)
                            yield return null;
                        yield break;
                    }

                    ModLog.Info("[VisualTest] Unknown postLoad step ignored: " + step);
                    break;
            }

            yield return null;
        }
    }
}
