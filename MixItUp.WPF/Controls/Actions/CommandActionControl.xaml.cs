﻿using Mixer.Base.Util;
using MixItUp.Base;
using MixItUp.Base.Actions;
using MixItUp.Base.Commands;
using MixItUp.Base.Themes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MixItUp.WPF.Controls.Actions
{
    public partial class CommandActionControl : ActionControlBase
    {
        private static readonly List<int> sampleFontSize = new List<int>() { 12, 24, 36, 48, 60, 72, 84, 96, 108, 120 };

        private CommandAction command;

        public CommandActionControl(ActionContainerControl containerControl) : base(containerControl) { InitializeComponent(); }

        public CommandActionControl(ActionContainerControl containerControl, CommandAction action) : this(containerControl) { this.command = action; }

        public override Task OnLoaded()
        {
            this.TypeComboBox.ItemsSource = EnumHelper.GetEnumNames<CommandActionTypeEnum>();

            this.CommandNameComboBox.ItemsSource = ChannelSession.PreMadeChatCommands.Union<PermissionsCommandBase>(ChannelSession.Settings.ChatCommands).OrderBy(c => c.Name);
            if (this.command != null)
            {
                this.TypeComboBox.SelectedValue = EnumHelper.GetEnumName(this.command.CommandActionType);
                this.CommandNameComboBox.SelectedItem = ChannelSession.PreMadeChatCommands.Union<PermissionsCommandBase>(ChannelSession.Settings.ChatCommands).FirstOrDefault(c => c.Name.Equals(this.command.CommandName));
                this.CommandArgumentsTextBox.Text = this.command.CommandArguments;
            }
            return Task.FromResult(0);
        }

        public override ActionBase GetAction()
        {
            CommandActionTypeEnum type = EnumHelper.GetEnumValueFromString<CommandActionTypeEnum>((string)this.TypeComboBox.SelectedItem);
            if (this.CommandNameComboBox.SelectedIndex >= 0)
            {
                PermissionsCommandBase command = (PermissionsCommandBase)this.CommandNameComboBox.SelectedItem;
                return new CommandAction(type, command, this.CommandArgumentsTextBox.Text);
            }
            return null;
        }

        private void CommandTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CommandActionTypeEnum type = EnumHelper.GetEnumValueFromString<CommandActionTypeEnum>((string)this.TypeComboBox.SelectedItem);
            switch (type)
            {
                case CommandActionTypeEnum.RunCommand:
                    this.CommandArgumentsTextBox.Visibility = Visibility.Visible;
                    break;
                case CommandActionTypeEnum.EnableDisableCommand:
                    this.CommandArgumentsTextBox.Visibility = Visibility.Collapsed;
                    break;
            }
        }
    }
}
