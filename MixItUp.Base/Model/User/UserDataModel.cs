﻿using Mixer.Base.Model.User;
using MixItUp.Base.Commands;
using MixItUp.Base.Model.Import.ScorpBot;
using MixItUp.Base.Model.Import.Streamlabs;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MixItUp.Base.Model.User
{
    [DataContract]
    public class UserDataModel : NotifyPropertyChangedBase, IEquatable<UserDataModel>
    {
        [DataMember]
        public Guid ID { get; set; } = Guid.NewGuid();

        #region Mixer

        [DataMember]
        public uint MixerID { get; set; }
        [DataMember]
        public string MixerUsername { get; set; }
        [DataMember]
        public uint MixerChannelID { get; set; }

        [DataMember]
        public DateTimeOffset? MixerAccountDate { get; set; }
        [DataMember]
        public DateTimeOffset? MixerFollowDate { get; set; }
        [DataMember]
        public DateTimeOffset? MixerSubscribeDate { get; set; }
        [DataMember]
        public UserFanProgressionModel MixerFanProgression { get; set; }

        #endregion Mixer

        [DataMember]
        public string CustomTitle { get; set; }

        [DataMember]
        public int ViewingMinutes { get; set; }
        [DataMember]
        public int OfflineViewingMinutes { get; set; }

        [DataMember]
        public LockedList<ChatCommand> CustomCommands { get; set; } = new LockedList<ChatCommand>();
        [DataMember]
        public CustomCommand EntranceCommand { get; set; }

        [DataMember]
        public bool IsCurrencyRankExempt { get; set; }
        [DataMember]
        public bool IsSparkExempt { get; set; }

        [DataMember]
        public string PatreonUserID { get; set; }

        [DataMember]
        public uint ModerationStrikes { get; set; }

        [DataMember]
        public uint TotalStreamsWatched { get; set; }
        [DataMember]
        public double TotalAmountDonated { get; set; }
        [DataMember]
        public uint TotalSparksSpent { get; set; }
        [DataMember]
        public uint TotalEmbersSpent { get; set; }
        [DataMember]
        public uint TotalSubsGifted { get; set; }
        [DataMember]
        public uint TotalSubsReceived { get; set; }
        [DataMember]
        public uint TotalChatMessageSent { get; set; }
        [DataMember]
        public uint TotalTimesTagged { get; set; }
        [DataMember]
        public uint TotalSkillsUsed { get; set; }
        [DataMember]
        public uint TotalCommandsRun { get; set; }
        [DataMember]
        public uint TotalMonthsSubbed { get; set; }

        public UserDataModel() { }

        public UserDataModel(uint id, string username)
            : this()
        {
            this.MixerID = id;
            this.MixerUsername = username;
        }

        public UserDataModel(UserModel user)
        {
            this.MixerID = user.id;
            this.MixerUsername = user.username;
        }

        public UserDataModel(UserViewModel user)
        {
            this.ID = user.ID;
            this.MixerID = user.MixerID;
            this.MixerUsername = user.Username;
        }

        public UserDataModel(ScorpBotViewer viewer)
        {
            this.MixerID = viewer.MixerID;
            this.MixerUsername = viewer.MixerUsername;
            this.ViewingMinutes = (int)(viewer.Hours * 60.0);
        }

        public UserDataModel(StreamlabsChatBotViewer viewer)
        {
            if (viewer.Platform == StreamingPlatformTypeEnum.Mixer)
            {
                this.MixerID = viewer.ID;
                this.MixerUsername = viewer.Name;
            }
            this.ViewingMinutes = (int)(viewer.Hours * 60.0);
        }

        [JsonIgnore]
        public string Username
        {
            get
            {
                if (this.MixerID > 0) { return this.MixerUsername; }
                return this.MixerUsername;
            }
        }

        [JsonIgnore]
        public string ViewingHoursString { get { return (this.ViewingMinutes / 60).ToString(); } }

        [JsonIgnore]
        public string ViewingMinutesString { get { return (this.ViewingMinutes % 60).ToString(); } }

        [JsonIgnore]
        public int ViewingHoursPart
        {
            get
            {
                return this.ViewingMinutes / 60;
            }
            set
            {
                this.ViewingMinutes = value * 60 + this.ViewingMinutesPart;
            }
        }

        [JsonIgnore]
        public int ViewingMinutesPart
        {
            get
            {
                return this.ViewingMinutes % 60;
            }
            set
            {
                int extraHours = value / 60;
                this.ViewingHoursPart += extraHours;
                this.ViewingMinutes = ViewingHoursPart * 60 + (value % 60);
                this.NotifyPropertyChanged(nameof(ViewingHoursPart));
            }
        }

        [JsonIgnore]
        public string ViewingTimeString { get { return string.Format("{0} Hours & {1} Mins", this.ViewingHoursString, this.ViewingMinutesString); } }

        [JsonIgnore]
        public string ViewingTimeShortString { get { return string.Format("{0}H & {1}M", this.ViewingHoursString, this.ViewingMinutesString); } }

        [JsonIgnore]
        public int PrimaryCurrency
        {
            get
            {
                UserCurrencyModel currency = ChannelSession.Settings.Currencies.Values.FirstOrDefault(c => !c.IsRank && c.IsPrimary);
                if (currency != null)
                {
                    return currency.GetAmount(this);
                }
                return 0;
            }
        }

        [JsonIgnore]
        public UserRankViewModel Rank
        {
            get
            {
                UserCurrencyModel currency = ChannelSession.Settings.Currencies.Values.FirstOrDefault(c => !c.IsRank && c.IsPrimary);
                if (currency != null)
                {
                    return currency.GetRank(this);
                }
                return UserCurrencyModel.NoRank;
            }
        }

        [JsonIgnore]
        public int PrimaryRankPoints
        {
            get
            {
                UserCurrencyModel currency = ChannelSession.Settings.Currencies.Values.FirstOrDefault(c => c.IsRank && c.IsPrimary);
                if (currency != null)
                {
                    return currency.GetAmount(this);
                }
                return 0;
            }
        }

        [JsonIgnore]
        public string PrimaryRankName
        {
            get
            {
                return this.Rank.Name;
            }
        }

        [JsonIgnore]
        public string PrimaryRankNameAndPoints
        {
            get
            {
                return string.Format("{0} - {1}", this.PrimaryRankName, this.PrimaryRankPoints);
            }
        }

        public void UpdateData(UserViewModel user)
        {
            this.ID = user.ID;
            this.MixerID = user.MixerID;
            this.MixerUsername = user.Username;
        }

        public override bool Equals(object obj)
        {
            if (obj is UserDataModel)
            {
                return this.Equals((UserDataModel)obj);
            }
            return false;
        }

        public bool Equals(UserDataModel other)
        {
            return this.ID.Equals(other.ID);
        }

        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }

        public override string ToString()
        {
            return this.Username;
        }
    }
}