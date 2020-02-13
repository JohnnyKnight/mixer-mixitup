﻿using MaterialDesignThemes.Wpf;
using MixItUp.Base.Model.Statistics;
using StreamingClient.Base.Util;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace MixItUp.WPF.Controls.Statistics
{
    /// <summary>
    /// Interaction logic for StatisticsOverviewControl.xaml
    /// </summary>
    public partial class StatisticsOverviewControl : LoadingControlBase
    {
        private PackIconKind icon;
        private StatisticDataTrackerModelBase dataTracker;

        public StatisticsOverviewControl(StatisticDataTrackerModelBase dataTracker)
        {
            InitializeComponent();

            this.DataContext = this.dataTracker = dataTracker;
        }

        public void HideName() { this.StatisticNameTextBlock.Visibility = Visibility.Collapsed; }

        protected override Task OnLoaded()
        {
            this.StatisticNameTextBlock.Text = this.dataTracker.Name;
            if (this.dataTracker.IsPackIcon)
            {
                this.PackIcon.Kind = this.icon = EnumHelper.GetEnumValueFromString<PackIconKind>(this.dataTracker.IconName);
            }

            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        await this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                this.RefreshOverview();
                            }
                            catch (Exception ex) { Logger.Log(ex); }
                        }));

                        await Task.Delay(60000);
                    }
                }
                catch (Exception ex) { Logger.Log(ex); }
            });

            return Task.FromResult(0);
        }

        protected override Task OnVisibilityChanged()
        {
            this.RefreshOverview();

            return Task.FromResult(0);
        }

        public void RefreshOverview()
        {
            this.StatisticOverviewDataTextBlock.Text = this.dataTracker.ToString();
        }
    }
}
