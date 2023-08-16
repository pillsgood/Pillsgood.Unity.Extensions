using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

            if (fieldInfo != null && IsNullable(fieldInfo))
            {
                __result += "?";
            }
        }

        private static bool IsNullable(FieldInfo field)
        {
            return IsNullableHelper(field.FieldType, field.DeclaringType, field.CustomAttributes);
        }

        private static bool IsNullableHelper(
            Type memberType,
            MemberInfo? declaringType,
            IEnumerable<CustomAttributeData> customAttributes)
        {
            if (memberType.IsValueType)
            {
                return Nullable.GetUnderlyingType(memberType) != null;
            }

            var nullable = customAttributes
                .FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");
            if (nullable != null && nullable.ConstructorArguments.Count == 1)
            {
                var attributeArgument = nullable.ConstructorArguments[0];
                if (attributeArgument.ArgumentType == typeof(byte[]))
                {
                    var args = (ReadOnlyCollection<CustomAttributeTypedArgument>)attributeArgument.Value!;
                    if (args.Count > 0 && args[0].ArgumentType == typeof(byte))
                    {
                        return (byte)args[0].Value! == 2;
                    }
                }
                else if (attributeArgument.ArgumentType == typeof(byte))
                {
                    return (byte)attributeArgument.Value! == 2;
                }
            }

            for (var type = declaringType; type != null; type = type.DeclaringType)
            {
                var context = type.CustomAttributes
                    .FirstOrDefault(x =>
                        x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute");
                if (context != null &&
                    context.ConstructorArguments.Count == 1 &&
                    context.ConstructorArguments[0].ArgumentType == typeof(byte))
                {
                    return (byte)context.ConstructorArguments[0].Value! == 2;
                }
            }

            // Couldn't find a suitable attribute
            return false;
        }
    }
}