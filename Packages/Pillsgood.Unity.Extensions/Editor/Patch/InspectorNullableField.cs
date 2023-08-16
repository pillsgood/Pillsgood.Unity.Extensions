using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Pillsgood.Unity.Extensions.Editor.Preferences;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace Pillsgood.Unity.Extensions.Editor.Patch
{
    [HarmonyPatch]
    internal static class InspectorNullableField
    {
        private class Options : UserOptions
        {
            [UserSetting("Inspector", "Show Nullable Fields")]
            public static readonly ISetting<bool> Enabled = Create("nullable_inspector.enabled", true);
        }

        [HarmonyReversePatch]
        [HarmonyPatch("UnityEditor.ScriptAttributeUtility", nameof(GetFieldInfoFromProperty))]
        private static FieldInfo? GetFieldInfoFromProperty(SerializedProperty property, out Type? type)
        {
            throw new InvalidOperationException();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SerializedProperty), nameof(SerializedProperty.displayName), MethodType.Getter)]
        private static void Postfix_SerializedProperty_GetDisplayName(SerializedProperty __instance, ref string __result)
        {
            if (!Options.Enabled.Value)
            {
                return;
            }

            var fieldInfo = GetFieldInfoFromProperty(__instance, out var type);
            if (type is null) return;
            if (type.IsValueType) return;

            if (fieldInfo != null && IsNullableMember(fieldInfo))
            {
                __result += "?";
            }
        }

        private static bool IsNullableMember(MemberInfo fieldInfo)
        {
            return fieldInfo.CustomAttributes.Any(x => x.AttributeType.Name is "NullableAttribute");
        }
    }
}