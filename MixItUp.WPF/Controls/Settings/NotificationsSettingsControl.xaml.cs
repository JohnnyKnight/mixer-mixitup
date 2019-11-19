﻿using MixItUp.Base;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MixItUp.WPF.Controls.Settings
{
    /// <summary>
    /// Interaction logic for NotificationsSettingsControl.xaml
    /// </summary>
    public partial class NotificationsSettingsControl : SettingsControlBase
    {
        private const string SoundFilePathFormat = "Assets\\Sounds\\{0}.mp3";

        private readonly string NoneSoundName = MixItUp.Base.Resources.None;
        private readonly string CustomSoundName = MixItUp.Base.Resources.Custom;

        private List<string> AvailableSounds;

        public NotificationsSettingsControl()
        {
            AvailableSounds = new List<string>() { NoneSoundName, "Ariel", "Carme", "Ceres", "Computer Chime", "Doorbell", "Elara", "Europa", "High Beeps", "Io",
            "Lapetus", "Level Up", "Low Beeps", "Rhea", "Robot SMS", "Salacia", "Tethys", "Titan", "Watch Alarm", CustomSoundName };

            InitializeComponent();

            this.ChatMessageComboBox.ItemsSource = this.AvailableSounds;
            this.ChatTaggedComboBox.ItemsSource = this.AvailableSounds;
            this.ChatWhisperComboBox.ItemsSource = this.AvailableSounds;
            this.ServiceConnectComboBox.ItemsSource = this.AvailableSounds;
            this.ServiceDisconnectComboBox.ItemsSource = this.AvailableSounds;

            this.AssignSelectedItem(this.ChatMessageComboBox, ChannelSession.Settings.NotificationChatMessageSoundFilePath);
            this.AssignSelectedItem(this.ChatTaggedComboBox, ChannelSession.Settings.NotificationChatTaggedSoundFilePath);
            this.AssignSelectedItem(this.ChatWhisperComboBox, ChannelSession.Settings.NotificationChatWhisperSoundFilePath);
            this.AssignSelectedItem(this.ServiceConnectComboBox, ChannelSession.Settings.NotificationServiceConnectSoundFilePath);
            this.AssignSelectedItem(this.ServiceDisconnectComboBox, ChannelSession.Settings.NotificationServiceDisconnectSoundFilePath);

            this.ChatMessageVolumeSlider.Value = ChannelSession.Settings.NotificationChatMessageSoundVolume;
            this.ChatTaggedVolumeSlider.Value = ChannelSession.Settings.NotificationChatTaggedSoundVolume;
            this.ChatWhisperVolumeSlider.Value = ChannelSession.Settings.NotificationChatWhisperSoundVolume;
            this.ServiceConnectVolumeSlider.Value = ChannelSession.Settings.NotificationServiceConnectSoundVolume;
            this.ServiceDisconnectVolumeSlider.Value = ChannelSession.Settings.NotificationServiceDisconnectSoundVolume;
        }

        protected override async Task InitializeInternal()
        {
            await base.InitializeInternal();
        }

        protected override async Task OnVisibilityChanged()
        {
            await this.InitializeInternal();
        }

        private void AssignSelectedItem(ComboBox comboBox, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                comboBox.SelectedItem = NoneSoundName;
            }
            else
            {
                foreach (string availableSound in this.AvailableSounds)
                {
                    if (string.Format(SoundFilePathFormat, availableSound).Equals(value))
                    {
                        comboBox.SelectedItem = availableSound;
                        return;
                    }
                }
                comboBox.SelectedItem = CustomSoundName;
            }
        }

        private string GetSoundFilePath(ComboBox comboBox, string currentValue)
        {
            string selectedSound = (string)comboBox.SelectedItem;
            if (!string.IsNullOrEmpty(selectedSound) && !selectedSound.Equals(NoneSoundName))
            {
                if (selectedSound.Equals(CustomSoundName))
                {
                    if (Path.IsPathRooted(currentValue))
                    {
                        return currentValue;
                    }

                    selectedSound = ChannelSession.Services.FileService.ShowOpenFileDialog(ChannelSession.Services.FileService.MusicFileFilter());
                    if (!string.IsNullOrEmpty(selectedSound))
                    {
                        return selectedSound;
                    }
                    else
                    {
                        comboBox.SelectedItem = NoneSoundName;
                    }
                }
                else
                {
                    return string.Format(SoundFilePathFormat, selectedSound);
                }
            }
            return null;
        }

        private void PlayChatMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ChannelSession.Settings.NotificationChatMessageSoundFilePath))
            {
                ChannelSession.Services.AudioService.Play(ChannelSession.Settings.NotificationChatMessageSoundFilePath, ChannelSession.Settings.NotificationChatMessageSoundVolume);
            }
        }

        private void ChatMessageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ChatMessageComboBox.SelectedIndex >= 0)
            {
                ChannelSession.Settings.NotificationChatMessageSoundFilePath = this.GetSoundFilePath(this.ChatMessageComboBox, ChannelSession.Settings.NotificationChatMessageSoundFilePath);
            }
        }

        private void PlayChatTaggedButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ChannelSession.Settings.NotificationChatTaggedSoundFilePath))
            {
                ChannelSession.Services.AudioService.Play(ChannelSession.Settings.NotificationChatTaggedSoundFilePath, ChannelSession.Settings.NotificationChatTaggedSoundVolume);
            }
        }

        private void ChatTaggedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ChatTaggedComboBox.SelectedIndex >= 0)
            {
                ChannelSession.Settings.NotificationChatTaggedSoundFilePath = this.GetSoundFilePath(this.ChatTaggedComboBox, ChannelSession.Settings.NotificationChatTaggedSoundFilePath);
            }
        }

        private void PlayChatWhisperButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ChannelSession.Settings.NotificationChatWhisperSoundFilePath))
            {
                ChannelSession.Services.AudioService.Play(ChannelSession.Settings.NotificationChatWhisperSoundFilePath, ChannelSession.Settings.NotificationChatWhisperSoundVolume);
            }
        }

        private void ChatWhisperComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ChatWhisperComboBox.SelectedIndex >= 0)
            {
                ChannelSession.Settings.NotificationChatWhisperSoundFilePath = this.GetSoundFilePath(this.ChatWhisperComboBox, ChannelSession.Settings.NotificationChatWhisperSoundFilePath);
            }
        }

        private void PlayServiceConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ChannelSession.Settings.NotificationServiceConnectSoundFilePath))
            {
                ChannelSession.Services.AudioService.Play(ChannelSession.Settings.NotificationServiceConnectSoundFilePath, ChannelSession.Settings.NotificationServiceConnectSoundVolume);
            }
        }

        private void ServiceConnectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ServiceConnectComboBox.SelectedIndex >= 0)
            {
                ChannelSession.Settings.NotificationServiceConnectSoundFilePath = this.GetSoundFilePath(this.ServiceConnectComboBox, ChannelSession.Settings.NotificationServiceConnectSoundFilePath);
            }
        }

        private void PlayServiceDisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ChannelSession.Settings.NotificationServiceDisconnectSoundFilePath))
            {
                ChannelSession.Services.AudioService.Play(ChannelSession.Settings.NotificationServiceDisconnectSoundFilePath, ChannelSession.Settings.NotificationServiceDisconnectSoundVolume);
            }
        }

        private void ServiceDisconnectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ServiceDisconnectComboBox.SelectedIndex >= 0)
            {
                ChannelSession.Settings.NotificationServiceDisconnectSoundFilePath = this.GetSoundFilePath(this.ServiceDisconnectComboBox, ChannelSession.Settings.NotificationServiceDisconnectSoundFilePath);
            }
        }

        private void ChatMessageVolumeSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ChannelSession.Settings.NotificationChatMessageSoundVolume = (int)this.ChatMessageVolumeSlider.Value;
        }

        private void ChatTaggedVolumeSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ChannelSession.Settings.NotificationChatTaggedSoundVolume = (int)this.ChatTaggedVolumeSlider.Value;
        }

        private void ChatWhisperVolumeSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ChannelSession.Settings.NotificationChatWhisperSoundVolume = (int)this.ChatWhisperVolumeSlider.Value;
        }

        private void ServiceConnectVolumeSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ChannelSession.Settings.NotificationServiceConnectSoundVolume = (int)this.ServiceConnectVolumeSlider.Value;
        }

        private void ServiceDisconnectVolumeSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ChannelSession.Settings.NotificationServiceDisconnectSoundVolume = (int)this.ServiceDisconnectVolumeSlider.Value;
        }
    }
}
