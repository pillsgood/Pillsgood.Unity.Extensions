using HarmonyLib;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Pillsgood.Unity.Extensions.Editor
{
    internal static class Extensions
    {
        [DidReloadScripts]
        private static void OnDomainReload()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            var harmony = new Harmony("com.pillsgood.extensions");
            harmony.PatchAll();
        }
    }
}