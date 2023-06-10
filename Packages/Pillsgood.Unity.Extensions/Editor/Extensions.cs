using System;
using HarmonyLib;
using Pillsgood.Unity.Extensions.Editor.Patch;
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