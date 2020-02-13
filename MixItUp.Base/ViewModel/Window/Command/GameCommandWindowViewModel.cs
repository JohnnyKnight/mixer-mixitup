﻿using MixItUp.Base.Commands;
using MixItUp.Base.Model.User;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MixItUp.Base.ViewModel.Window.Command
{
    public class GameTypeListing
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public GameTypeListing(string name, string description)
        {
            this.Name = name;
            this.Description = description;
        }
    }

    public class GameCommandWindowViewModel : WindowViewModelBase
    {
        public event EventHandler<GameTypeListing> GameTypeSelected;

        public GameCommandBase GameCommand { get; private set; }

        public UserCurrencyModel DefaultCurrency { get; private set; }

        public ObservableCollection<GameTypeListing> GameListings { get; private set; } = new ObservableCollection<GameTypeListing>();
        public GameTypeListing SelectedGameType
        {
            get { return this.selectedGameType; }
            set
            {
                this.selectedGameType = value;
                this.NotifyPropertyChanged();
            }
        }
        private GameTypeListing selectedGameType;

        public GameTypeListing GameTypeToMake { get; private set; }

        public bool GameSelected { get { return this.GameCommand != null || this.GameTypeToMake != null; } }
        public bool GameNotSelected { get { return !this.GameSelected; } }

        public ICommand GameTypeSelectedCommand { get; private set; }

        public GameCommandWindowViewModel(GameCommandBase gameCommand)
        {
            this.GameCommand = gameCommand;
        }

        public GameCommandWindowViewModel()
        {
            this.DefaultCurrency = ChannelSession.Settings.Currencies.Values.FirstOrDefault(c => !c.IsRank && c.IsPrimary);
            if (this.DefaultCurrency == null)
            {
                this.DefaultCurrency = ChannelSession.Settings.Currencies.Values.FirstOrDefault(c => !c.IsRank);
            }

            this.GameListings.Add(new GameTypeListing("Beach Ball", "The Beach Ball game hits a beach ball to a player, who must then hit it off to someone else. If the person who the beach ball is going towards does not respond in time, then that user misses the hit and the game ends. The user who hit the beach ball last is dubbed the winner ($username) and the user who failed to hit the beach ball is dubbed the loser ($targetusername). By default, the passing of the beach ball is done randomly to a new user, but you can optionally allow the passer to specify who it gets hit to."
                + Environment.NewLine + Environment.NewLine + "\tEX: !beachball" + Environment.NewLine + "\tEX: !beachball <TARGET USER>"));
            this.GameListings.Add(new GameTypeListing("Bet", "The Bet game allows a user to start a bet amongst all users over which option is correct. Each user who participates must specify which options you expect to win and how much they wish to bet. Once the time limit is completed, a user can specify what the correct answer is to the bet and all correct users will be rewarded. Additionally, you can specify what user roles can start & answer this game to ensure it isn't abused, such as Moderators or higher only."
                + Environment.NewLine + Environment.NewLine + "\tEX: !bet" + Environment.NewLine + "\tEX: !bet <OPTION #> 100" + Environment.NewLine + "\tEX: !bet answer <OPTION #>"));
            this.GameListings.Add(new GameTypeListing("Bid", "The Bid game allows a user to start a bidding competition amongst all users to win a prize or special privilege. A user must bid at least 1 currency amount higher than the highest bid to become the leading bidder. When a user is outbid, they receive their bet currency back and the highest bidder when time runs out wins. Additionally, you can specify what user roles can start this game to ensure it isn't abused, such as Moderators or higher only."
                + Environment.NewLine + Environment.NewLine + "\tEX: !bid 100"));
            this.GameListings.Add(new GameTypeListing("Coin Pusher", "The Coin Pusher game allows a user to deposit a specific amount of currency into a machine with the chance for a large payout. A minimum amount of currency must be inserted into the machine to allow for a payout and a minimum & maximum payout percentage can be specified when a payout occurs."
                + Environment.NewLine + Environment.NewLine + "\tEX: !pusher 100"));
            this.GameListings.Add(new GameTypeListing("Duel", "The Duel game challenges the specified user to a winner-takes-all for the bet amount. If successful, the user takes the bet amount from the specified user. If failed, the specified user takes the bet amount from the user."
                + Environment.NewLine + Environment.NewLine + "\tEX: !duel <USERNAME> 100"));
            this.GameListings.Add(new GameTypeListing("Hangman", "The Hangman game allows users to guess the letter combination of a random word to win an accumulated pot of currency. Every failed guess puts the required currency amount into the pot and adds a failure, while a correct guess adds the letter. The person to guess the last missing letter gets the whole pot." + Environment.NewLine + Environment.NewLine +
                "Users have the ability to see the status of the game to see how much the pot is and what has been guessed already."
                + Environment.NewLine + Environment.NewLine + "\tEX: !hangman <LETTER>"));
            this.GameListings.Add(new GameTypeListing("Heist", "The Heist game allows a user to start a group activity for users to individually bet when they participate. Each user has their own individual chance to succeed and win back more or fail and lose their bet."
                + Environment.NewLine + Environment.NewLine + "\tEX: !heist 100"));
            this.GameListings.Add(new GameTypeListing("Hitman", "The Hitman game allows a user to start a winner-takes-all bet amongst all entered users. After the initial time limit, a hitman with a specific name will appear in chat. If a user types the hitman's name in chat within the time limit, they win the entire pot. Otherwise, everyone loses their money."
                + Environment.NewLine + Environment.NewLine + "\tEX: !hitman 100"));
            this.GameListings.Add(new GameTypeListing("Hot Potato", "The Hot Potato game gives a ticking potato bomb to a player, who must then pass it off to someone else before it explodes. The user who passed the potato bomb last is dubbed the winner ($username) and the user who had the potato bomb when it exploded is dubbed the loser ($targetusername). By default, the passing of the potato bomb is done randomly to a new user, but you can optionally allow the passer to specify who it gets tossed to."
                + Environment.NewLine + Environment.NewLine + "\tEX: !potato" + Environment.NewLine + "\tEX: !potato <TARGET USER>"));
            this.GameListings.Add(new GameTypeListing("Lock Box", "The Lock Box game allows you to guess the combination of a locked box using the numbers 0 - 9 that contains a large amount of currency. Every failed guess puts the required currency amount into the box and gives a hint to the combination, while a correct guess gets all the currency inside." + Environment.NewLine + Environment.NewLine +
                "Users have the ability to see the status of the lock box to see how much is in it and inspect the lock box for a hint as to the combination."
                + Environment.NewLine + Environment.NewLine + "\tEX: !lockbox 100"));
            this.GameListings.Add(new GameTypeListing("Pickpocket", "The Pickpocket game attempts to steal currency from a specified user. If successful, the user steals the bet amount from the specified user. If failed, they lose the bet amount."
                + Environment.NewLine + Environment.NewLine + "\tEX: !pickpocket <USERNAME> 100"));
            this.GameListings.Add(new GameTypeListing("Roulette", "The Roulette game allows a user to start a group betting game amongst all users to bet on a specific number or name. If the game is based on a number range, then a user can select any number in that range. If the game is based on a series of names, a user can select any one of the names. All users who select the winning bet type get the payout for their bet, everyone else losses their bet."
                + Environment.NewLine + Environment.NewLine + "\tEX: !roulette <NUMBER> 100"));
            this.GameListings.Add(new GameTypeListing("Russian Roulette", "The Russian Roulette game allows a user to start a winner-takes-all bet amongst all entered users. By default, the user that starts the game specifies how much the bet is and all subsequent users must bet that amount to join, with all winners of the game splitting the total payout equally."
                + Environment.NewLine + Environment.NewLine + "\tEX: !rr 100\t\tAfter Start: !rr"));
            this.GameListings.Add(new GameTypeListing("Slot Machine", "The Slot Machine game picks a random set of 3 symbols from a pre-defined list and selects any outcome that matches those symbols. Besides selecting a payout for each outcome, you can also run a customized command for each outcome."
                + Environment.NewLine + Environment.NewLine + "\tEX: !slots 100"));
            this.GameListings.Add(new GameTypeListing("Spin", "The Spin game picks a random number and selects an outcome based on that number. Besides selecting a payout for each outcome, you can also run a customized command for each outcome."
                + Environment.NewLine + Environment.NewLine + "\tEX: !spin 100"));
            this.GameListings.Add(new GameTypeListing("Steal", "The Steal game picks a random user in chat and attempts to steal currency from them. If successful, the user steals the bet amount from the random user. If failed, they lose the bet amount."
                + Environment.NewLine + Environment.NewLine + "\tEX: !steal 100"));
            this.GameListings.Add(new GameTypeListing("Treasure Defense", "The Treasure Defense game allows a user to start a winner-takes-all bet amongst all entered users. By default, the user that starts the game specifies how much the bet is and all subsequent users must bet that amount to join, with all winners of the game splitting the total payout equally." + Environment.NewLine + Environment.NewLine +
                "After the time limit is up, a user from the participants is selected to be King. All other users are either then selected to be Knights or Thieves. It is the job of the King to pick one participant in hopes that they are a Knight to defend the treasure they found. If the King selects a Knight, then the King & Knights share the winnings. If the King selects a Thief, then the Thieves share the winnings."
                + Environment.NewLine + Environment.NewLine + "\tEX: !treasure <AMOUNT>" + Environment.NewLine + "\tEX: !treasure" + Environment.NewLine + "\tEX: !treasure <SELECTED USER>"));
            this.GameListings.Add(new GameTypeListing("Trivia", "The Trivia game picks a random question from a pre-determined list and asks all users to select the correct answer from a list of possible ones. All users that selected the correct answer are awarded a payout."
                + Environment.NewLine + Environment.NewLine + "\tEX: !trivia"));
            this.GameListings.Add(new GameTypeListing("Vending Machine", "The Vending Machine game picks a random number and selects an outcome based on that number. Unlike the Spin game, the Vending Machine game doesn't have a payout for each outcome and instead is more focused on an \"action\" occurring with each outcome, such as a sound effect, image, or a specialized effect."
                + Environment.NewLine + Environment.NewLine + "\tEX: !vend" + Environment.NewLine + Environment.NewLine + "Game Designed By: https://mixer.com/InsertCoinTheater"));
            this.GameListings.Add(new GameTypeListing("Volcano", "The Volcano game allows a user to deposit a specific amount of currency into a volcano with the chance for a personal payout and a payout for all users in chat. The volcano goes through 3 stages as more and more currency is deposited into it and a different set of Deposit & Status commands are used depending on what stage the Volcano is at." + Environment.NewLine + Environment.NewLine +
                "Once the volcano reaches stage 3, each subsequent deposit has a chance to trigger an eruption. When an eruption occurs, the user who triggered it gets a specialized payout for them. After the eruption, all users have a chance to collect erupted currency during the collection time limit. After the collection is done, the volcano contents resets back to 0."
                + Environment.NewLine + Environment.NewLine + "\tEX: !volcano 100" + Environment.NewLine + Environment.NewLine + "Game Designed By: https://mixer.com/InsertCoinTheater"));
            this.GameListings.Add(new GameTypeListing("Word Scramble", "The Word Scramble game allows a user to start a winner-takes-all bet amongst all entered users. After the initial time limit, a scrambled word will appear in chat. If a user types the unscrambled word in chat within the time limit, they win the entire pot. Otherwise, everyone loses their money."
                + Environment.NewLine + Environment.NewLine + "\tEX: !scramble 100"));


            this.GameTypeSelectedCommand = this.CreateCommand((parameter) =>
            {
                this.GameTypeToMake = this.SelectedGameType;

                this.NotifyPropertyChanged("GameSelected");
                this.NotifyPropertyChanged("GameNotSelected");

                this.GameTypeSelected(this, this.GameTypeToMake);

                return Task.FromResult(0);
            });
        }
    }
}
