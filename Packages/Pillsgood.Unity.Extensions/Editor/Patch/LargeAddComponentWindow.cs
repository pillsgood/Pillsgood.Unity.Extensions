using System.Linq;
using HarmonyLib;
using Pillsgood.Unity.Extensions.Editor.Preferences;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace Pillsgood.Unity.Extensions.Editor.Patch
{
    [HarmonyPatch]
    internal static class LargeAddComponentWindow
    {
        private class Options : UserOptions
        {
            [UserSetting("Add Component Window", "Enabled")]
            public static readonly ISetting<bool> Enabled = Create("extensions.component_window.enabled", true);

            [UserSetting("Add Component Window", "Width Modifier")]
            public static readonly ISetting<float> WidthModifier = Create("extensions.component_window.width_modifier", 3f);

            [UserSetting("Add Component Window", "Height Modifier")]
            public static readonly ISetting<float> HeightModifier = Create("extensions.component_window.height_modifier", 1.5f);
        }

        [HarmonyPostfix]
        [HarmonyPatch("UnityEditor.AddComponent.AddComponentWindow", "CalculateWindowSize")]
        private static void Postfix_AddComponentWindow_CalculateWindowSize(ref Vector2 __result)
        {
            if (!Options.Enabled.Value)
            {
                return;
            }

            var width = __result.x * Options.WidthModifier.Value;
            var height = __result.y * Options.HeightModifier.Value;

            __result = new Vector2(width, height);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EditorWindow), "ShowAsDropDownFitToScreen")]
        private static void Prefix_EditorWindow_ShowAsDropDownFitToScreen(ref Rect buttonRect, Vector2 windowSize)
        {
            if (!Options.Enabled.Value)
            {
                return;
            }

            if (windowSize.x > buttonRect.width)
            {
                buttonRect.x += (buttonRect.width - windowSize.x) / 2f;
            }
        }
    }
}