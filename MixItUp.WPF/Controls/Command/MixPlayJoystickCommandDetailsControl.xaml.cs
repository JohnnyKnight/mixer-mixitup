﻿using Mixer.Base.Model.MixPlay;
using MixItUp.Base;
using MixItUp.Base.Commands;
using MixItUp.Base.Services;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.MixPlay;
using MixItUp.Base.ViewModel.Requirement;
using MixItUp.WPF.Controls.Dialogs;
using MixItUp.WPF.Util;
using StreamingClient.Base.Util;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MixItUp.WPF.Controls.Command
{
    /// <summary>
    /// Interaction logic for MixPlayJoystickCommandDetailsControl.xaml
    /// </summary>
    public partial class MixPlayJoystickCommandDetailsControl : CommandDetailsControlBase
    {
        public MixPlayGameModel Game { get; private set; }
        public MixPlayGameVersionModel Version { get; private set; }
        public MixPlayJoystickControlModel Control { get; private set; }

        private MixPlayJoystickCommand command;

        public MixPlayJoystickCommandDetailsControl(MixPlayGameModel game, MixPlayGameVersionModel version, MixPlayControlViewModel command)
            : this(game, version, command.Joystick)
        {
            this.command = (MixPlayJoystickCommand)command.Command;
        }

        public MixPlayJoystickCommandDetailsControl(MixPlayGameModel game, MixPlayGameVersionModel version, MixPlayJoystickControlModel control)
        {
            this.Game = game;
            this.Version = version;
            this.Control = control;

            InitializeComponent();
        }

        public override Task Initialize()
        {
            this.Requirements.HideSettingsRequirement();

            this.JoystickSetupComboBox.ItemsSource = EnumHelper.GetEnumNames<MixPlayJoystickSetupType>();
            this.UpKeyComboBox.ItemsSource = this.RightKeyComboBox.ItemsSource = this.DownKeyComboBox.ItemsSource = this.LeftKeyComboBox.ItemsSource = EnumHelper.GetEnumNames<InputKeyEnum>().OrderBy(s => s);

            this.JoystickDeadZoneTextBox.Text = "20";
            this.MouseMovementMultiplierTextBox.Text = "1.0";

            this.Requirements.HideCooldownRequirement();

            if (this.Control != null)
            {
                this.NameTextBox.Text = this.Control.controlID;
            }

            if (this.command != null)
            {
                this.JoystickSetupComboBox.SelectedItem = EnumHelper.GetEnumName(this.command.SetupType);
                this.JoystickDeadZoneTextBox.Text = (this.command.DeadZone * 100).ToString();
                this.MouseMovementMultiplierTextBox.Text = this.command.MouseMovementMultiplier.ToString();
            }

            return Task.FromResult(0);
        }

        public override async Task<bool> Validate()
        {
            if (this.JoystickSetupComboBox.SelectedIndex < 0)
            {
                await DialogHelper.ShowMessage("A Joystick Setup must be selected");
                return false;
            }
            MixPlayJoystickSetupType setup = EnumHelper.GetEnumValueFromString<MixPlayJoystickSetupType>((string)this.JoystickSetupComboBox.SelectedItem);

            if (!int.TryParse(this.JoystickDeadZoneTextBox.Text, out int deadzone) || deadzone < 0 || deadzone > 100)
            {
                await DialogHelper.ShowMessage("A valid Joystick Dead Zone must be entered between 0 & 100");
                return false;
            }

            if (setup == MixPlayJoystickSetupType.MouseMovement)
            {
                if (!double.TryParse(this.MouseMovementMultiplierTextBox.Text, out double mouseMultiplier) || mouseMultiplier < 1.0)
                {
                    await DialogHelper.ShowMessage("A valid Movement Multiplier must be entered that is 1.0 or greater");
                    return false;
                }
            }

            if (!await this.Requirements.Validate())
            {
                return false;
            }

            return true;
        }

        public override CommandBase GetExistingCommand() { return this.command; }

        public override async Task<CommandBase> GetNewCommand()
        {
            if (await this.Validate())
            {
                MixPlayJoystickSetupType setup = EnumHelper.GetEnumValueFromString<MixPlayJoystickSetupType>((string)this.JoystickSetupComboBox.SelectedItem);

                RequirementViewModel requirements = this.Requirements.GetRequirements();

                if (this.command == null)
                {
                    this.command = new MixPlayJoystickCommand(this.Game, this.Control, requirements);
                    ChannelSession.Settings.MixPlayCommands.Add(this.command);
                }
                this.command.InitializeAction();

                this.command.SetupType = setup;
                this.command.DeadZone = (double.Parse(this.JoystickDeadZoneTextBox.Text) / 100.0);
                this.command.MappedKeys.Clear();

                if (setup == MixPlayJoystickSetupType.MouseMovement)
                {
                    this.command.MouseMovementMultiplier = double.Parse(this.MouseMovementMultiplierTextBox.Text);
                }
                else if (setup == MixPlayJoystickSetupType.MapToIndividualKeys)
                {
                    if (this.UpKeyComboBox.SelectedIndex >= 0) { this.command.MappedKeys.Add(EnumHelper.GetEnumValueFromString<InputKeyEnum>((string)this.UpKeyComboBox.SelectedItem)); } else { this.command.MappedKeys.Add(null); }
                    if (this.RightKeyComboBox.SelectedIndex >= 0) { this.command.MappedKeys.Add(EnumHelper.GetEnumValueFromString<InputKeyEnum>((string)this.RightKeyComboBox.SelectedItem)); } else { this.command.MappedKeys.Add(null); }
                    if (this.DownKeyComboBox.SelectedIndex >= 0) { this.command.MappedKeys.Add(EnumHelper.GetEnumValueFromString<InputKeyEnum>((string)this.DownKeyComboBox.SelectedItem)); } else { this.command.MappedKeys.Add(null); }
                    if (this.LeftKeyComboBox.SelectedIndex >= 0) { this.command.MappedKeys.Add(EnumHelper.GetEnumValueFromString<InputKeyEnum>((string)this.LeftKeyComboBox.SelectedItem)); } else { this.command.MappedKeys.Add(null); }
                }

                this.command.Unlocked = this.UnlockedControl.Unlocked;
                this.command.Requirements = requirements;

                return this.command;
            }
            return null;
        }

        private string GetSelectedIndex(InputKeyEnum? inputKey)
        {
            if (inputKey.HasValue)
            {
                return EnumHelper.GetEnumName(inputKey.Value);
            }

            return null;
        }

        private void JoystickSetupComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (this.JoystickSetupComboBox.SelectedIndex >= 0)
            {
                MixPlayJoystickSetupType setup = EnumHelper.GetEnumValueFromString<MixPlayJoystickSetupType>((string)this.JoystickSetupComboBox.SelectedItem);
                if (setup == MixPlayJoystickSetupType.MapToIndividualKeys)
                {
                    this.UpKeyComboBox.IsEnabled = this.RightKeyComboBox.IsEnabled = this.DownKeyComboBox.IsEnabled = this.LeftKeyComboBox.IsEnabled = true;
                    this.UpKeyComboBox.SelectedItem = GetSelectedIndex(this.command?.MappedKeys.ElementAtOrDefault(0));
                    this.RightKeyComboBox.SelectedItem = GetSelectedIndex(this.command?.MappedKeys.ElementAtOrDefault(1));
                    this.DownKeyComboBox.SelectedItem = GetSelectedIndex(this.command?.MappedKeys.ElementAtOrDefault(2));
                    this.LeftKeyComboBox.SelectedItem = GetSelectedIndex(this.command?.MappedKeys.ElementAtOrDefault(3));
                }
                else
                {
                    this.UpKeyComboBox.IsEnabled = this.RightKeyComboBox.IsEnabled = this.DownKeyComboBox.IsEnabled = this.LeftKeyComboBox.IsEnabled = false;
                    if (setup == MixPlayJoystickSetupType.WASD)
                    {
                        this.UpKeyComboBox.SelectedItem = EnumHelper.GetEnumName(InputKeyEnum.W);
                        this.RightKeyComboBox.SelectedItem = EnumHelper.GetEnumName(InputKeyEnum.D);
                        this.DownKeyComboBox.SelectedItem = EnumHelper.GetEnumName(InputKeyEnum.S);
                        this.LeftKeyComboBox.SelectedItem = EnumHelper.GetEnumName(InputKeyEnum.A);
                    }
                    else if (setup == MixPlayJoystickSetupType.DirectionalArrows || setup == MixPlayJoystickSetupType.MouseMovement)
                    {
                        this.UpKeyComboBox.SelectedItem = EnumHelper.GetEnumName(InputKeyEnum.Up);
                        this.RightKeyComboBox.SelectedItem = EnumHelper.GetEnumName(InputKeyEnum.Right);
                        this.DownKeyComboBox.SelectedItem = EnumHelper.GetEnumName(InputKeyEnum.Down);
                        this.LeftKeyComboBox.SelectedItem = EnumHelper.GetEnumName(InputKeyEnum.Left);
                    }
                }

                this.MouseMovementMultiplierTextBox.Visibility = (setup == MixPlayJoystickSetupType.MouseMovement) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private async void TestSetupButton_Click(object sender, RoutedEventArgs e)
        {
            CommandBase command = await this.GetNewCommand();
            if (command != null)
            {
                await DialogHelper.ShowCustomTimed(new InteractiveJoystickSetupTestDialogControl((MixPlayJoystickCommand)command), 16000);
            }
        }
    }
}