using System;
using HarmonyLib;
using Pillsgood.Unity.Extensions.Editor.Patch;
using UnityEditor.Callbacks;

namespace Pillsgood.Unity.Extensions.Editor
{
    internal static class Extensions
    {
        private static Harmony? _harmony;

        public static void Harmony(Action<Harmony> action)
        {
            _harmony ??= new Harmony("com.pillsgood.extensions");
            action(_harmony);
        }

        [DidReloadScripts]
        private static void OnDomainReload() => Harmony(harmony =>
        {
            if (LargeAddComponentWindow.Options.Enabled.Value)
            {
                harmony.CreateClassProcessor(typeof(LargeAddComponentWindow)).Patch();
            }
        });
    }
}