using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using Pillsgood.Unity.Extensions.Editor.Preferences;
using UnityEditor;
using UnityEditor.Compilation;
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

            [UserSetting(Category, "Show Namespace For Well Known Types")]
            public static readonly ISetting<bool> ShowNamespaceForWellKnownTypes = Create("extensions.titlebar.show_ns_for_well_known_types", false);

            [UserSetting(Category, "Match Height For Well Known Types")]
            public static readonly ISetting<bool> MatchHeightForWellKnownTypes = Create("extensions.titlebar.match_height_for_well_known_types", true);
        }

        [HarmonyReversePatch]
        [HarmonyPatch("UnityEditor.ObjectNames+InspectorTitles", "TryGet")]
        private static bool TryGetInspectorTitle(Type objectType, out string title)
        {
            throw new InvalidOperationException();
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(EditorStyles), "inspectorTitlebar", MethodType.Getter)]
        private static GUIStyle GetInspectorTitlebar()
        {
            throw new InvalidOperationException();
        }

        [HarmonyPrefix, UsedImplicitly]
        [HarmonyPatch(typeof(ObjectNames), nameof(ObjectNames.GetInspectorTitle), typeof(Object), typeof(bool))]
        private static bool Prefix_ObjectNames_GetInspectorTitle(
            ref string __result,
            Object obj,
            bool multiObjectEditing)
        {
            if (!Options.Enabled.Value) return true;

            if (ShouldShowNamespaceForType(obj))
            {
                var type = obj.GetType();
                __result = $"<b>{GetTypeName(type)}</b>";

                var color = ColorUtility.ToHtmlStringRGB(Options.NamespaceFontColor.Value);
                var fontSize = Options.NamespaceFontSize.Value;
                __result += $"\n<color=#{color}><size={fontSize}>{type.Namespace}</size></color>";

                return string.IsNullOrEmpty(__result);
            }

            return true;
        }

        private static string GetTypeName(Type type)
        {
            if (Options.ShowNamespaceForWellKnownTypes.Value &&
                TryGetInspectorTitle(type, out var title))
            {
                return title;
            }

            return type.Name;
        }

        private static bool ShouldShowNamespaceForType(Object? obj)
        {
            if (!Options.ShowNamespace.Value)
            {
                return false;
            }

            Func<Object?, bool> test = Options.ShowNamespaceForWellKnownTypes.Value
                ? static obj => obj is Behaviour
                : static obj => obj is MonoBehaviour;

            if (test(obj))
            {
                if (Options.ShowNamespaceForWellKnownTypes.Value)
                {
                    return true;
                }

                var type = obj!.GetType();
                return !TryGetInspectorTitle(type, out _);
            }

            return false;
        }

        [HarmonyPatch(typeof(EditorGUI), "DoInspectorTitlebar")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpile_EditorGUI_DoInspectorTitlebar(
            IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ldarg_S, 3);
            yield return new CodeInstruction(OpCodes.Ldarg_S, 5);

            yield return CodeInstruction.Call(
                (Expression<Func<Object[], GUIStyle, GUIStyle>>)((objs, style) => UpdateInspectorTitlebar(objs, style)));
            yield return new CodeInstruction(OpCodes.Starg_S, 5);

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Stloc_0)
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 3);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return CodeInstruction.Call(
                        (Expression<Func<Object[], GUIStyle, GUIStyle>>)((objs, style) => UpdateInspectorTitlebarText(objs, style)));
                    yield return new CodeInstruction(OpCodes.Stloc_0);
                    continue;
                }

                yield return instruction;
            }
        }

        [HarmonyPatch(
            typeof(EditorGUILayout),
            nameof(EditorGUILayout.InspectorTitlebar),
            typeof(bool), typeof(UnityEditor.Editor))]
        [HarmonyPrefix]
        private static bool Prefix_EditorGUILayout_InspectorTitlebar(
            ref bool __result,
            bool foldout,
            UnityEditor.Editor editor)
        {
            var inspectorTitlebar = GetInspectorTitlebar();
            inspectorTitlebar = UpdateInspectorTitlebar(editor.targets, inspectorTitlebar);
            __result = EditorGUI.InspectorTitlebar(GUILayoutUtility.GetRect(GUIContent.none, inspectorTitlebar), foldout, editor);
            return false;
        }

        private static GUIStyle UpdateInspectorTitlebar(Object[] objs, GUIStyle style)
        {
            if (!Options.Enabled.Value) return style;

            var obj = objs.FirstOrDefault();

            if (obj is Transform) return style;

            var shouldMatchHeight = Options.MatchHeightForWellKnownTypes.Value;
            var shouldShowNamespaceForType = ShouldShowNamespaceForType(obj);

            if (shouldMatchHeight || shouldShowNamespaceForType)
            {
                return new GUIStyle(style)
                {
                    fixedHeight = Options.Height.Value
                };
            }

            return style;
        }

        private static GUIStyle UpdateInspectorTitlebarText(Object[] objects, GUIStyle style)
        {
            if (!Options.Enabled.Value) return style;

            var obj = objects.FirstOrDefault();

            if (obj is Transform) return style;

            var shouldMatchHeight = Options.MatchHeightForWellKnownTypes.Value;
            var shouldShowNamespaceForType = ShouldShowNamespaceForType(obj);
            if (!shouldMatchHeight && !shouldShowNamespaceForType)
            {
                return style;
            }

            var fixedHeight = style.fixedHeight;
            var fontStyle = style.fontStyle;

            if (shouldShowNamespaceForType)
            {
                fontStyle = FontStyle.Normal;
            }

            if (shouldMatchHeight || shouldShowNamespaceForType)
            {
                fixedHeight = Options.Height.Value - 2;
            }

            return new GUIStyle(style)
            {
                fixedHeight = fixedHeight,
                fontStyle = fontStyle
            };
        }
    }
}