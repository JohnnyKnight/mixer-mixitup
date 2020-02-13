﻿using Mixer.Base.Interactive;
using Mixer.Base.Model.MixPlay;
using MixItUp.Base;
using MixItUp.Base.Util;
using MixItUp.WPF.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MixItUp.WPF.Windows.Interactive
{
    public class InteractiveBoardSize
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    /// <summary>
    /// Interaction logic for InteractiveBoardDesignerWindow.xaml
    /// </summary>
    public partial class InteractiveBoardDesignerWindow : LoadingWindowBase
    {
        private const int LargeWidth = 80;
        private const int LargeHeight = 22;

        private const int MediumWidth = 40;
        private const int MediumHeight = 25;

        private const int SmallWidth = 30;
        private const int SmallHeight = 40;

        private MixPlayGameListingModel gameListing;
        private List<MixPlaySceneModel> scenes = new List<MixPlaySceneModel>();
        private MixPlaySceneModel selectedScene = null;

        private List<InteractiveBoardSize> boardSizes = new List<InteractiveBoardSize>();
        private InteractiveBoardSize selectedBoardSize = null;

        private bool[,] boardBlocks;
        private int blockWidthHeight;

        public InteractiveBoardDesignerWindow(MixPlayGameListingModel gameListing, IEnumerable<MixPlaySceneModel> scenes)
        {
            InitializeComponent();

            this.Initialize(this.StatusBar);

            this.gameListing = gameListing;
            this.scenes = new List<MixPlaySceneModel>(scenes);

            this.SizeChanged += InteractiveBoardDesignerWindow_SizeChanged;
        }

        protected override Task OnLoaded()
        {
            this.boardSizes.Add(new InteractiveBoardSize() { Name = "Large", Width = LargeWidth, Height = LargeHeight });
            this.boardSizes.Add(new InteractiveBoardSize() { Name = "Medium", Width = MediumWidth, Height = MediumHeight });
            this.boardSizes.Add(new InteractiveBoardSize() { Name = "Small", Width = SmallWidth, Height = SmallHeight });
            this.selectedBoardSize = this.boardSizes.First();

            this.SceneComboBox.ItemsSource = this.scenes;
            this.BoardSizeComboBox.ItemsSource = this.boardSizes;

            this.SceneComboBox.SelectedIndex = 0;
            this.BoardSizeComboBox.SelectedIndex = 0;

            this.GameNameTextBox.Text = this.gameListing.name;

            return base.OnLoaded();
        }

        private void SaveChangesButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //foreach (MixPlaySceneModel scene in this.interactiveScenes)
            //{
            //    if (scene.buttons.Count == 0 && scene.joysticks.Count == 0)
            //    {
            //        MessageBoxHelper.ShowDialog("The following scene does not contain any controls: " + scene.sceneID);
            //        return;
            //    }
            //}

            //if (this.selectedGame == null)
            //{
            //    await this.Window.RunAsyncOperation(async () =>
            //    {
            //        await ChannelSession.MixerConnection.Interactive.UpdateInteractiveGame(this.selectedGame);
            //        await ChannelSession.MixerConnection.Interactive.UpdateMixPlayGameVersion(this.selectedGameVersion);
            //    });
            //}

            //await this.RefreshSelectedInteractiveGame();
        }

        public async Task Save()
        {
            if (string.IsNullOrEmpty(this.GameNameTextBox.Text))
            {
                await DialogHelper.ShowMessage("A name must be specified for the game");
                return;
            }

            MixPlayGameListingModel game = await this.RunAsyncOperation(async () =>
            {
                MixPlaySceneModel defaultScene = MixPlayGameHelper.CreateDefaultScene();
                return await ChannelSession.MixerUserConnection.CreateMixPlayGame(ChannelSession.MixerChannel, ChannelSession.MixerUser, this.GameNameTextBox.Text, MixPlayGameHelper.CreateDefaultScene());
            });

            if (game == null)
            {
                await DialogHelper.ShowMessage("Failed to create game");
                return;
            }
        }

        public void RefreshScene()
        {
            this.InteractiveBoardCanvas.Children.Clear();
            this.boardBlocks = new bool[this.selectedBoardSize.Width, this.selectedBoardSize.Height];

            int perBlockWidth = (int)this.InteractiveBoardCanvas.ActualWidth / (this.selectedBoardSize.Width);
            int perBlockHeight = (int)this.InteractiveBoardCanvas.ActualHeight / (this.selectedBoardSize.Height);
            this.blockWidthHeight = Math.Min(perBlockWidth, perBlockHeight);

            foreach (MixPlayControlModel control in this.selectedScene.allControls)
            {
                this.BlockOutControlArea(control);
            }

            for (int w = 0; w < this.selectedBoardSize.Width; w++)
            {
                for (int h = 0; h < this.selectedBoardSize.Height; h++)
                {
                    if (!this.boardBlocks[w, h])
                    {
                        this.RenderRectangle(w, h, 1, 1, Brushes.Blue);
                    }
                }
            }
        }

        private void BlockOutControlArea(MixPlayControlModel control)
        {
            MixPlayControlPositionModel position = control.position.FirstOrDefault(p => p.size.Equals(this.selectedBoardSize.Name.ToLower()));
            for (int w = 0; w < position.width; w++)
            {
                for (int h = 0; h < position.height; h++)
                {
                    this.boardBlocks[position.x + w, position.y + h] = true;
                }
            }
            this.RenderRectangle(position.x, position.y, position.width, position.height, Brushes.Black);

            TextBlock textBlock = new TextBlock();
            textBlock.Text = control.controlID;
            textBlock.Foreground = Brushes.Black;
            textBlock.TextWrapping = TextWrapping.Wrap;
            textBlock.TextAlignment = TextAlignment.Center;
            this.AddElementToCanvas(textBlock, position.x + 1, position.y + 1, position.width - 2, position.height - 2);
        }

        private void RenderRectangle(int x, int y, int width, int height, Brush color)
        {
            Rectangle rect = new Rectangle();
            rect.Stroke = color;
            rect.StrokeThickness = 1;
            this.AddElementToCanvas(rect, x, y, width, height);
        }

        private void AddElementToCanvas(FrameworkElement element, int x, int y, int width, int height)
        {
            element.Width = width * this.blockWidthHeight;
            element.Height = height * this.blockWidthHeight;
            Canvas.SetLeft(element, x * this.blockWidthHeight);
            Canvas.SetTop(element, y * this.blockWidthHeight);
            this.InteractiveBoardCanvas.Children.Add(element);
        }

        private void SceneComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.selectedScene != null && this.SceneComboBox.SelectedIndex >= 0)
            {
                this.selectedScene = (MixPlaySceneModel)this.SceneComboBox.SelectedItem;
                this.RefreshScene();
            }
        }

        private void BoardSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.selectedScene != null && this.BoardSizeComboBox.SelectedIndex >= 0)
            {
                this.selectedBoardSize = (InteractiveBoardSize)this.BoardSizeComboBox.SelectedItem;
                this.RefreshScene();
            }
        }

        private void InteractiveBoardDesignerWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.selectedScene != null)
            {
                this.RefreshScene();
            }
        }
    }
}
