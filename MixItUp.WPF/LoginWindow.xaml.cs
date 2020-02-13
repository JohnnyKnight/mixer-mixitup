﻿using Mixer.Base.Model.User;
using MixItUp.Base;
using MixItUp.Base.Model.API;
using MixItUp.Base.Model.Settings;
using MixItUp.Base.Services.External;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using MixItUp.WPF.Windows;
using MixItUp.WPF.Windows.Wizard;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MixItUp.WPF
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : LoadingWindowBase
    {
        private MixItUpUpdateModel currentUpdate;
        private bool updateFound = false;

        private ObservableCollection<SettingsV2Model> streamerSettings = new ObservableCollection<SettingsV2Model>();
        private ObservableCollection<SettingsV2Model> moderatorSettings = new ObservableCollection<SettingsV2Model>();

        public LoginWindow()
        {
            InitializeComponent();

            this.Initialize(this.StatusBar);
        }

        protected override async Task OnLoaded()
        {
            GlobalEvents.OnShowMessageBox += GlobalEvents_OnShowMessageBox;

            this.Title += " - v" + Assembly.GetEntryAssembly().GetName().Version.ToString();

            this.ExistingStreamerComboBox.ItemsSource = streamerSettings;
            this.ModeratorChannelComboBox.ItemsSource = moderatorSettings;

            await this.CheckForUpdates();

            foreach (SettingsV2Model setting in (await ChannelSession.Services.Settings.GetAllSettings()).OrderBy(s => s.Name))
            {
                if (setting.IsStreamer)
                {
                    this.streamerSettings.Add(setting);
                }
                else
                {
                    this.moderatorSettings.Add(setting);
                }
            }

            if (this.streamerSettings.Count > 0)
            {
                this.ExistingStreamerComboBox.Visibility = Visibility.Visible;
                this.streamerSettings.Add(new SettingsV2Model() { MixerChannelID = 0, Name = MixItUp.Base.Resources.NewStreamer });
                if (this.streamerSettings.Count() == 2)
                {
                    this.ExistingStreamerComboBox.SelectedIndex = 0;
                }
            }

            if (this.moderatorSettings.Count == 1)
            {
                this.ModeratorChannelComboBox.SelectedIndex = 0;
            }

            if (ChannelSession.AppSettings.AutoLogInAccount > 0)
            {
                var allSettings = this.streamerSettings.ToList();
                allSettings.AddRange(this.moderatorSettings);

                SettingsV2Model autoLogInSettings = allSettings.FirstOrDefault(s => s.MixerChannelID == ChannelSession.AppSettings.AutoLogInAccount);
                if (autoLogInSettings != null)
                {
                    await Task.Delay(5000);

                    if (!updateFound)
                    {
                        if (await this.ExistingSettingLogin(autoLogInSettings))
                        {
                            LoadingWindowBase newWindow = null;
                            if (ChannelSession.Settings.ReRunWizard)
                            {
                                newWindow = new NewUserWizardWindow();
                            }
                            else
                            {
                                newWindow = new MainWindow();
                            }
                            ShowMainWindow(newWindow);
                            this.Hide();
                            this.Close();
                            return;
                        }
                    }
                }
            }

            await base.OnLoaded();
        }

        private async void StreamerLoginButton_Click(object sender, RoutedEventArgs e)
        {
            await this.RunAsyncOperation(async () =>
            {
                if (this.ExistingStreamerComboBox.Visibility == Visibility.Visible)
                {
                    if (this.ExistingStreamerComboBox.SelectedIndex >= 0)
                    {
                        SettingsV2Model setting = (SettingsV2Model)this.ExistingStreamerComboBox.SelectedItem;
                        if (setting.MixerChannelID == 0)
                        {
                            await this.NewStreamerLogin();
                        }
                        else
                        {
                            if (await this.ExistingSettingLogin(setting))
                            {
                                LoadingWindowBase newWindow = null;
                                if (ChannelSession.Settings.ReRunWizard)
                                {
                                    newWindow = new NewUserWizardWindow();
                                }
                                else
                                {
                                    newWindow = new MainWindow();
                                }
                                ShowMainWindow(newWindow);
                                this.Hide();
                                this.Close();
                                return;
                            }
                        }
                    }
                    else
                    {
                        await DialogHelper.ShowMessage(MixItUp.Base.Resources.LoginErrorNoStreamerAccount);
                    }
                }
                else
                {
                    await this.NewStreamerLogin();
                }
            });
        }

        private void ModeratorChannelComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.ModeratorLoginButton_Click(this, new RoutedEventArgs());
            }
        }

        private async void ModeratorLoginButton_Click(object sender, RoutedEventArgs e)
        {
            await this.RunAsyncOperation(async () =>
            {
                if (string.IsNullOrEmpty(this.ModeratorChannelComboBox.Text))
                {
                    await DialogHelper.ShowMessage(MixItUp.Base.Resources.LoginErrorNoChannelName);
                    return;
                }

                if (this.ModeratorChannelComboBox.SelectedIndex >= 0)
                {
                    SettingsV2Model setting = (SettingsV2Model)this.ModeratorChannelComboBox.SelectedItem;
                    if (!await this.ExistingSettingLogin(setting))
                    {
                        return;
                    }
                }
                else
                {
                    if (!await this.ShowLicenseAgreement())
                    {
                        return;
                    }

                    ExternalServiceResult result = await ChannelSession.ConnectMixerUser(isStreamer: false);
                    if (!result.Success)
                    {
                        await DialogHelper.ShowMessage(result.Message);
                        return;
                    }
                }

                if (await ChannelSession.InitializeSession(this.ModeratorChannelComboBox.Text))
                {
                    IEnumerable<UserWithGroupsModel> users = await ChannelSession.MixerUserConnection.GetUsersWithRoles(ChannelSession.MixerChannel, UserRoleEnum.Mod);
                    if (users.Any(uwg => uwg.id.Equals(ChannelSession.MixerUser.id)) || ChannelSession.IsDebug())
                    {
                        ShowMainWindow(new MainWindow());
                        this.Hide();
                        this.Close();
                    }
                    else
                    {
                        await DialogHelper.ShowMessage(MixItUp.Base.Resources.LoginErrorNotModerator);
                    }
                }
                else
                {
                    await DialogHelper.ShowMessage(MixItUp.Base.Resources.LoginErrorFailedToAuthenticate);
                }
            });
        }

        private async Task CheckForUpdates()
        {
            this.currentUpdate = await ChannelSession.Services.MixItUpService.GetLatestUpdate();
            if (this.currentUpdate != null)
            {
                if (ChannelSession.AppSettings.PreviewProgram)
                {
                    MixItUpUpdateModel previewUpdate = await ChannelSession.Services.MixItUpService.GetLatestPreviewUpdate();
                    if (previewUpdate != null && previewUpdate.SystemVersion >= this.currentUpdate.SystemVersion)
                    {
                        this.currentUpdate = previewUpdate;
                    }
                }

                if (this.currentUpdate.SystemVersion > Assembly.GetEntryAssembly().GetName().Version)
                {
                    updateFound = true;
                    UpdateWindow window = new UpdateWindow(this.currentUpdate);
                    window.Show();
                }
            }
        }

        private async Task<bool> ExistingSettingLogin(SettingsV2Model setting)
        {
            ExternalServiceResult result = await ChannelSession.ConnectUser(setting);
            if (result.Success)
            {
                if (await ChannelSession.InitializeSession(setting.IsStreamer ? null : setting.Name))
                {
                    return true;
                }
            }
            else
            {
                await DialogHelper.ShowMessage(result.Message);
            }
            return false;
        }

        private async Task NewStreamerLogin()
        {
            if (await this.ShowLicenseAgreement())
            {
                ShowMainWindow(new NewUserWizardWindow());
                this.Hide();
                this.Close();
            }
        }

        private async void GlobalEvents_OnShowMessageBox(object sender, string message)
        {
            await this.RunAsyncOperation(async () =>
            {
                await DialogHelper.ShowMessage(message);
            });
        }

        private Task<bool> ShowLicenseAgreement()
        {
            LicenseAgreementWindow window = new LicenseAgreementWindow();
            TaskCompletionSource<bool> task = new TaskCompletionSource<bool>();
            window.Owner = Application.Current.MainWindow;
            window.Closed += (s, a) => task.SetResult(window.Accepted);
            window.Show();
            window.Focus();
            return task.Task;
        }
    }
}