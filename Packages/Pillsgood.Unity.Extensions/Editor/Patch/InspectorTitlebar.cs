using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using Pillsgood.Unity.Extensions.Editor.Preferences;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pillsgood.Unity.Extensions.Editor.Patch
{
    [HarmonyPatch]
    internal static class InspectorTitlebar
    {
        private class Options : UserOptions
        {
            private const string Category = "Inspector Title Bar";

            [UserSetting(Category, "Enabled")]
            public static readonly ISetting<bool> Enabled = Create("extensions.titlebar.enabled", true);

            [UserSetting(Category, "Height")]
            public static readonly ISetting<float> Height = Create("extensions.titlebar.fixed_height", 40f);

            [UserSetting(Category, "Show Namespace")]
            public static readonly ISetting<bool> ShowNamespace = Create("extensions.titlebar.show_namespace", true);

            [UserSetting(Category, "Namespace Font Size")]
            public static readonly ISetting<int> NamespaceFontSize = Create("extensions.titlebar.ns_font_size", 9);

            [UserSetting(Category, "Namespace Font Color")]
            public static readonly ISetting<Color> NamespaceFontColor = Create("extensions.titlebar.ns_font_color", (Color)new Color32(0x9d, 0x9d, 0x9d, 0xff));
        }

        [HarmonyReversePatch]
        [HarmonyPatch("UnityEditor.ObjectNames+InspectorTitles", "TryGet")]
        private static bool TryGetInspectorTitle(Type objectType, out string title)
        {
            throw new InvalidOperationException();
        }

        [HarmonyPrefix, UsedImplicitly]
        [HarmonyPatch(typeof(ObjectNames), nameof(ObjectNames.GetInspectorTitle), typeof(Object), typeof(bool))]
        private static bool Prefix_ObjectNames_GetInspectorTitle(
            ref string __result,
            Object obj,
            bool multiObjectEditing,
            out bool __state)
        {
            __state = false;
            if (!Options.Enabled.Value) return true;

            if (obj is MonoBehaviour monoBehaviour)
            {
                var type = monoBehaviour.GetType();

                if (TryGetInspectorTitle(type, out _)) return true;

                __result = $"<b>{type.Name}</b>";

                if (Options.ShowNamespace.Value)
                {
                    var color = ColorUtility.ToHtmlStringRGB(Options.NamespaceFontColor.Value);
                    var fontSize = Options.NamespaceFontSize.Value;
                    __result += $"\n<color=#{color}><size={fontSize}>{type.Namespace}</size></color>";
                }

                __state = !string.IsNullOrEmpty(__result);
                return !__state;
            }

            return true;
        }

        [HarmonyPostfix, UsedImplicitly]
        [HarmonyPatch(typeof(ObjectNames), nameof(ObjectNames.GetInspectorTitle), typeof(Object), typeof(bool))]
        private static void Postfix_ObjectNames_GetInspectorTitle(
            ref string __result,
            bool __state)
        {
            if (!__state) __result = $"<b>{__result}</b>";
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [HarmonyPostfix, UsedImplicitly]
        [HarmonyPatch(typeof(EditorStyles), nameof(inspectorTitlebar), MethodType.Getter)]
        private static void inspectorTitlebar(ref GUIStyle __result)
        {
            if (!Options.Enabled.Value) return;

            __result.fixedHeight = Options.Height.Value;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [HarmonyPostfix, UsedImplicitly]
        [HarmonyPatch(typeof(EditorStyles), nameof(inspectorTitlebarText), MethodType.Getter)]
        private static void inspectorTitlebarText(ref GUIStyle __result)
        {
            if (!Options.Enabled.Value) return;

            __result.fixedHeight = Options.Height.Value - 2;
            __result.fontStyle = FontStyle.Normal;
        }
    }
}