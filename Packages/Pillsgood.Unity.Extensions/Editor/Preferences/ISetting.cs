using UnityEditor.SettingsManagement;

namespace Pillsgood.Unity.Extensions.Editor.Preferences
{
    internal interface ISetting<out T> : IUserSetting
    {
        T Value { get; }
    }
}