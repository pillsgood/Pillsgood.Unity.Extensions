using UnityEditor;
using UnityEditor.SettingsManagement;

namespace Pillsgood.Unity.Extensions.Editor.Preferences
{
    internal class OptionsProvider
    {
        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new UserSettingsProvider("Preferences/Extensions", UserOptions.Settings, new[]
            {
                typeof(OptionsProvider).Assembly
            });
        }
    }
}