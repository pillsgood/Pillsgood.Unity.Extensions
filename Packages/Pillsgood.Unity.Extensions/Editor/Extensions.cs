using System;
using System.Threading;
using HarmonyLib;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

namespace Pillsgood.Unity.Extensions.Editor
{
    internal static class Extensions
    {
        private static CancellationTokenSource? _cts;

        [DidReloadScripts]
        private static void OnDomainReload()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            var cancellationToken = _cts.Token;
            PatchAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task PatchAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;

            var harmony = new Harmony("com.pillsgood.extensions");
            harmony.PatchAll();
        }
    }
}