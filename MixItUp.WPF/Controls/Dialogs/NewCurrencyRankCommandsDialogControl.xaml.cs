﻿using MixItUp.Base.Commands;
using MixItUp.Base.Model.User;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace MixItUp.WPF.Controls.Dialogs
{
    public class NewCurrencyRankCommand
    {
        public bool AddCommand { get; set; }
        public string Description { get; set; }
        public ChatCommand Command { get; set; }

        public NewCurrencyRankCommand(string description, ChatCommand command)
        {
            this.AddCommand = true;
            this.Description = description;
            this.Command = command;
        }
    }

    /// <summary>
    /// Interaction logic for NewCurrencyRankCommandsDialogControl.xaml
    /// </summary>
    public partial class NewCurrencyRankCommandsDialogControl : UserControl
    {
        public ObservableCollection<NewCurrencyRankCommand> commands = new ObservableCollection<NewCurrencyRankCommand>();

        private UserCurrencyModel currency;

        public NewCurrencyRankCommandsDialogControl(UserCurrencyModel currency, IEnumerable<NewCurrencyRankCommand> commands)
        {
            this.DataContext = this.currency = currency;

            InitializeComponent();

            this.NewCommandsItemsControl.ItemsSource = this.commands;
            foreach (NewCurrencyRankCommand command in commands)
            {
                this.commands.Add(command);
            }

            this.Loaded += NewCurrencyRankCommandsDialogControl_Loaded;
        }

        private void NewCurrencyRankCommandsDialogControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.NewCurrencyTextBlock.Visibility = (!this.currency.IsRank) ? Visibility.Visible : Visibility.Collapsed;
            this.NewRankTextBlock.Visibility = (this.currency.IsRank) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
