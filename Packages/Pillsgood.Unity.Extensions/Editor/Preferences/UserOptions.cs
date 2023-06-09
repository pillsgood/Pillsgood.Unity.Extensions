using System;
using UnityEditor;
using UnityEditor.SettingsManagement;

namespace Pillsgood.Unity.Extensions.Editor.Preferences
{
    internal class UserOptions
    {
        public static readonly Settings Settings = new(new[]
        {
            new UserSettingsRepository()
        });

        protected static ISetting<T> Create<T>(string key, T value)
        {
            return new DefaultSetting<T>(Settings, key, value, SettingsScope.User);
        }

        protected static ISetting<T> Create<T>(string key, T value, Action<T> onChange)
        {
            return new NotifyingSetting<T>(Settings, key, value, SettingsScope.User, onChange);
        }

        private class DefaultSetting<T> : UserSetting<T>, ISetting<T>
        {
            public DefaultSetting(Settings settings, string key, T value, SettingsScope scope) : base(settings, key, value, scope)
            {
            }

            public T Value => value;
        }

        private class NotifyingSetting<T> : UserSetting<T>, ISetting<T>
        {
            private readonly Action<T> _onChange;

            public NotifyingSetting(Settings settings, string key, T value, SettingsScope scope, Action<T> onChange) : base(settings, key, value, scope)
            {
                _onChange = onChange;
                Settings.afterSettingsSaved -= OnAfterSettingsSaved;
                Settings.afterSettingsSaved += OnAfterSettingsSaved;
            }

            private void OnAfterSettingsSaved()
            {
                _onChange(Value);
            }

            public T Value => value;
        }
    }
}