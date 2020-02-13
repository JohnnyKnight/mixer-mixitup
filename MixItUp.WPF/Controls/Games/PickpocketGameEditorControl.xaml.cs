﻿using MixItUp.Base.Commands;
using MixItUp.Base.Model.User;
using MixItUp.Base.ViewModel.Controls.Games;
using MixItUp.Base.ViewModel.Requirement;
using System.Threading.Tasks;

namespace MixItUp.WPF.Controls.Games
{
    /// <summary>
    /// Interaction logic for PickpocketGameEditorControl.xaml
    /// </summary>
    public partial class PickpocketGameEditorControl : GameEditorControlBase
    {
        private PickpocketGameEditorControlViewModel viewModel;
        private PickpocketGameCommand existingCommand;

        public PickpocketGameEditorControl(UserCurrencyModel currency)
        {
            InitializeComponent();

            this.viewModel = new PickpocketGameEditorControlViewModel(currency);
        }

        public PickpocketGameEditorControl(PickpocketGameCommand command)
        {
            InitializeComponent();

            this.existingCommand = command;
            this.viewModel = new PickpocketGameEditorControlViewModel(command);
        }

        public override async Task<bool> Validate()
        {
            if (!await this.CommandDetailsControl.Validate())
            {
                return false;
            }
            return await this.viewModel.Validate();
        }

        public override void SaveGameCommand()
        {
            this.viewModel.SaveGameCommand(this.CommandDetailsControl.GameName, this.CommandDetailsControl.ChatTriggers, this.CommandDetailsControl.GetRequirements());
        }

        protected override async Task OnLoaded()
        {
            this.DataContext = this.viewModel;
            await this.viewModel.OnLoaded();

            if (this.existingCommand != null)
            {
                this.CommandDetailsControl.SetDefaultValues(this.existingCommand);
            }
            else
            {
                this.CommandDetailsControl.SetDefaultValues("Pickpocket", "pickpocket", CurrencyRequirementTypeEnum.MinimumAndMaximum, 10, 1000);
            }
            await base.OnLoaded();
        }
    }
}
