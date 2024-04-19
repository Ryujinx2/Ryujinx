using Avalonia.Collections;
using Ryujinx.Common.Configuration;
using Ryujinx.UI.Common.Configuration;
using System;
using System.Linq;

namespace Ryujinx.Ava.UI.ViewModels.Settings
{
    public class SettingsUIViewModel : BaseModel
    {
        public event Action DirtyEvent;

        private bool _enableDiscordIntegration;
        public bool EnableDiscordIntegration
        {
            get => _enableDiscordIntegration;
            set
            {
                _enableDiscordIntegration = value;
                DirtyEvent?.Invoke();
            }
        }

        private bool _checkUpdatesOnStart;
        public bool CheckUpdatesOnStart
        {
            get => _checkUpdatesOnStart;
            set
            {
                _checkUpdatesOnStart = value;
                DirtyEvent?.Invoke();
            }
        }

        private bool _showConfirmExit;
        public bool ShowConfirmExit
        {
            get => _showConfirmExit;
            set
            {
                _showConfirmExit = value;
                DirtyEvent?.Invoke();
            }
        }

        private int _hideCursor;
        public int HideCursor
        {
            get => _hideCursor;
            set
            {
                _hideCursor = value;
                DirtyEvent?.Invoke();
            }
        }

        private int _baseStyleIndex;
        public int BaseStyleIndex
        {
            get => _baseStyleIndex;
            set
            {
                _baseStyleIndex = value;
                DirtyEvent?.Invoke();
            }
        }

        public bool DirsChanged;

        public AvaloniaList<string> GameDirectories { get; set; }

        public SettingsUIViewModel()
        {
            ConfigurationState config = ConfigurationState.Instance;

            GameDirectories = new();

            EnableDiscordIntegration = config.EnableDiscordIntegration;
            CheckUpdatesOnStart = config.CheckUpdatesOnStart;
            ShowConfirmExit = config.ShowConfirmExit;
            HideCursor = (int)config.HideCursor.Value;

            GameDirectories.Clear();
            GameDirectories.AddRange(config.UI.GameDirs.Value);
            GameDirectories.CollectionChanged += (_, _) => DirtyEvent?.Invoke();

            BaseStyleIndex = config.UI.BaseStyle == "Light" ? 0 : 1;
        }

        public bool CheckIfModified(ConfigurationState config)
        {
            bool isDirty = false;

            DirsChanged = !config.UI.GameDirs.Value.SequenceEqual(GameDirectories);

            isDirty |= config.EnableDiscordIntegration.Value != EnableDiscordIntegration;
            isDirty |= config.CheckUpdatesOnStart.Value != CheckUpdatesOnStart;
            isDirty |= config.ShowConfirmExit.Value != ShowConfirmExit;
            isDirty |= config.HideCursor.Value != (HideCursorMode)HideCursor;
            isDirty |= DirsChanged;
            isDirty |= config.UI.BaseStyle.Value != (BaseStyleIndex == 0 ? "Light" : "Dark");

            return isDirty;
        }

        public void Save(ConfigurationState config)
        {
            config.EnableDiscordIntegration.Value = EnableDiscordIntegration;
            config.CheckUpdatesOnStart.Value = CheckUpdatesOnStart;
            config.ShowConfirmExit.Value = ShowConfirmExit;
            config.HideCursor.Value = (HideCursorMode)HideCursor;
            config.UI.GameDirs.Value = GameDirectories.ToList();
            config.UI.BaseStyle.Value = BaseStyleIndex == 0 ? "Light" : "Dark";
        }
    }
}
