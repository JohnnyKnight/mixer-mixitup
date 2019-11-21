﻿using Mixer.Base.Model.MixPlay;
using MixItUp.Base;
using MixItUp.Base.Actions;
using MixItUp.Base.Commands;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Requirement;
using MixItUp.WPF.Controls.Actions;
using MixItUp.WPF.Util;
using MixItUp.WPF.Windows.Command;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MixItUp.WPF.Controls.Command
{
    /// <summary>
    /// Interaction logic for BasicInteractiveTextBoxCommandEditorControl.xaml
    /// </summary>
    public partial class BasicInteractiveTextBoxCommandEditorControl : CommandEditorControlBase
    {
        private CommandWindow window;

        private BasicCommandTypeEnum commandType;
        private MixPlayGameModel game;
        private MixPlayGameVersionModel version;
        private MixPlaySceneModel scene;
        private MixPlayTextBoxControlModel textBox;

        private MixPlayTextBoxCommand command;

        private ActionControlBase actionControl;

        public BasicInteractiveTextBoxCommandEditorControl(CommandWindow window, MixPlayGameModel game, MixPlayGameVersionModel version, MixPlayTextBoxCommand command)
        {
            this.window = window;
            this.game = game;
            this.version = version;
            this.command = command;

            InitializeComponent();
        }

        public BasicInteractiveTextBoxCommandEditorControl(CommandWindow window, MixPlayGameModel game, MixPlayGameVersionModel version, MixPlaySceneModel scene,
            MixPlayTextBoxControlModel textBox, BasicCommandTypeEnum commandType)
        {
            this.window = window;
            this.game = game;
            this.version = version;
            this.scene = scene;
            this.textBox = textBox;
            this.commandType = commandType;

            InitializeComponent();
        }

        public override CommandBase GetExistingCommand() { return this.command; }

        protected override async Task OnLoaded()
        {
            this.TextValueSpecialIdentifierTextBlock.Text = SpecialIdentifierStringBuilder.InteractiveTextBoxTextEntrySpecialIdentifierHelpText;

            if (this.command != null)
            {
                if (this.game != null)
                {
                    this.scene = this.version.controls.scenes.FirstOrDefault(s => s.sceneID.Equals(this.command.SceneID));
                    this.textBox = this.command.TextBox;
                }

                this.SparkCostTextBox.Text = this.command.TextBox.cost.ToString();
                this.UseChatModerationCheckBox.IsChecked = this.command.UseChatModeration;

                if (this.command.Actions.First() is ChatAction)
                {
                    this.actionControl = new ChatActionControl((ChatAction)this.command.Actions.First());
                }
                else if (this.command.Actions.First() is SoundAction)
                {
                    this.actionControl = new SoundActionControl((SoundAction)this.command.Actions.First());
                }
            }
            else
            {
                this.SparkCostTextBox.Text = this.textBox.cost.ToString();

                if (this.commandType == BasicCommandTypeEnum.Chat)
                {
                    this.actionControl = new ChatActionControl(null);
                }
                else if (this.commandType == BasicCommandTypeEnum.Sound)
                {
                    this.actionControl = new SoundActionControl(null);
                }
            }

            this.ActionControlControl.Content = this.actionControl;

            await base.OnLoaded();
        }

        private async void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveAndClose(false);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveAndClose(true);
        }

        private async Task SaveAndClose(bool isBasic)
        {
            await this.window.RunAsyncOperation((System.Func<Task>)(async () =>
            {
                int sparkCost = 0;
                if (!int.TryParse(this.SparkCostTextBox.Text, out sparkCost) || sparkCost < 0)
                {
                    await DialogHelper.ShowMessage("Spark cost must be 0 or greater");
                    return;
                }

                ActionBase action = this.actionControl.GetAction();
                if (action == null)
                {
                    if (this.actionControl is ChatActionControl)
                    {
                        await DialogHelper.ShowMessage("The chat message must not be empty");
                    }
                    else if (this.actionControl is SoundActionControl)
                    {
                        await DialogHelper.ShowMessage(MixItUp.Base.Resources.EmptySoundFilePath);
                    }
                    return;
                }

                RequirementViewModel requirements = new RequirementViewModel();

                if (this.command == null)
                {
                    this.command = new MixPlayTextBoxCommand(this.game, this.scene, this.textBox, requirements);
                    ChannelSession.Settings.MixPlayCommands.Add(this.command);
                }

                this.command.TextBox.cost = sparkCost;
                await ChannelSession.MixerStreamerConnection.UpdateMixPlayGameVersion(this.version);

                this.command.UseChatModeration = this.UseChatModerationCheckBox.IsChecked.GetValueOrDefault();
                this.command.IsBasic = isBasic;
                this.command.Actions.Clear();
                this.command.Actions.Add(action);

                await ChannelSession.SaveSettings();

                this.window.Close();

                if (!isBasic)
                {
                    await Task.Delay(250);
                    CommandWindow window = new CommandWindow(new InteractiveTextBoxCommandDetailsControl(this.game, this.version, this.command));
                    window.CommandSaveSuccessfully += (sender, cmd) => this.CommandSavedSuccessfully(cmd);
                    window.Show();
                }
            }));
        }
    }
}
