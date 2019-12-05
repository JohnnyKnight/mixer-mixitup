﻿using Mixer.Base.Model.Channel;
using Mixer.Base.Model.Chat;
using Mixer.Base.Model.MixPlay;
using Mixer.Base.Model.User;
using MixItUp.Base.Model;
using MixItUp.Base.Model.MixPlay;
using MixItUp.Base.Model.User;
using MixItUp.Base.Services;
using MixItUp.Base.Util;
using Newtonsoft.Json;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.ViewModel.User
{
    public enum MixerRoleEnum
    {
        Banned,

        User = 10,

        Pro = 20,

        Partner = 25,

        Follower = 30,

        Regular = 35,

        Subscriber = 40,

        GlobalMod = 48,

        Mod = 50,

        ChannelEditor = 55,

        Staff = 60,

        Streamer = 70,

        Custom = 99,
    }

    public enum AgeRatingEnum
    {
        Family,
        Teen,
        [Name("18+")]
        Adult,
    }

    public static class UserWithGroupsModelExtensions
    {
        public static DateTimeOffset? GetSubscriberDate(this UserWithGroupsModel userGroups)
        {
            return userGroups.GetCreatedDateForGroupIfCurrent(EnumHelper.GetEnumName(MixerRoleEnum.Subscriber));
        }
    }

    [DataContract]
    public class UserViewModel : IEquatable<UserViewModel>, IComparable<UserViewModel>
    {
        public const string DefaultAvatarLink = "https://mixer.com/_latest/assets/images/main/avatars/default.png";
        public const string UserAvatarLinkFormat = "https://mixer.com/api/v1/users/{0}/avatar?w=128&h=128";

        public static IEnumerable<MixerRoleEnum> SelectableBasicUserRoles()
        {
            List<MixerRoleEnum> roles = new List<MixerRoleEnum>(EnumHelper.GetEnumList<MixerRoleEnum>());
            roles.Remove(MixerRoleEnum.GlobalMod);
            roles.Remove(MixerRoleEnum.Banned);
            roles.Remove(MixerRoleEnum.Custom);
            return roles;
        }

        public static IEnumerable<MixerRoleEnum> SelectableAdvancedUserRoles()
        {
            return UserViewModel.SelectableBasicUserRoles();
        }

        [DataMember]
        public uint ID { get; set; }

        [DataMember]
        public StreamingPlatformTypeEnum Platform { get; set; }

        [DataMember]
        public string UserName { get; set; }

        [DataMember]
        public uint ChannelID { get; set; }

        [DataMember]
        public DateTimeOffset? MixerAccountDate { get; set; }

        [DataMember]
        public DateTimeOffset? FollowDate { get; set; }

        [DataMember]
        public DateTimeOffset? MixerSubscribeDate { get; set; }

        [DataMember]
        public int ChatOffenses { get; set; }

        [DataMember]
        public int Sparks { get; set; }

        [DataMember]
        public UserFanProgressionModel FanProgression { get; set; }

        [DataMember]
        public uint CurrentViewerCount { get; set; }

        [DataMember]
        public bool IgnoreForQueries { get; set; }

        [DataMember]
        public bool IsInChat { get; set; }

        [DataMember]
        public LockedDictionary<string, MixPlayParticipantModel> InteractiveIDs { get; set; }

        [DataMember]
        public string InteractiveGroupID { get; set; }

        [DataMember]
        public bool IsInInteractiveTimeout { get; set; }

        [DataMember]
        public string TwitterURL { get; set; }

        [DataMember]
        public PatreonCampaignMember PatreonUser { get; set; }

        public UserViewModel()
        {
            this.InteractiveIDs = new LockedDictionary<string, MixPlayParticipantModel>();
        }

        public UserViewModel(UserModel user) : this(user.id, user.username)
        {
            this.SetMixerUserDetails(user);
        }

        public UserViewModel(ChannelModel channel) : this(channel.id, channel.token) { }

        public UserViewModel(ChatUserModel user) : this(user.userId.GetValueOrDefault(), user.userName, user.userRoles) { this.IsInChat = true; }

        public UserViewModel(ChatMessageEventModel messageEvent) : this(messageEvent.user_id, messageEvent.user_name, messageEvent.user_roles) { this.IsInChat = true; }

        public UserViewModel(ChatMessageUserModel chatUser) : this(chatUser.user_id, chatUser.user_name, chatUser.user_roles) { this.IsInChat = true; }

        public UserViewModel(MixPlayParticipantModel participant) : this(participant.userID, participant.username) { this.SetInteractiveDetails(participant); }

        public UserViewModel(uint id, string username) : this(id, username, new string[] { }) { }

        public UserViewModel(UserDataViewModel user) : this(user.ID, user.UserName) { }

        public UserViewModel(uint id, string username, string[] userRoles)
            : this()
        {
            this.ID = id;
            this.UserName = username;

            this.Data.UpdateData(this);

            this.SetMixerRoles(userRoles);
        }

        public string AvatarLink { get { return string.Format(UserAvatarLinkFormat, this.ID); } }

        public string SubscriberBadgeLink { get { return (ChannelSession.MixerChannel.badge != null) ? ChannelSession.MixerChannel.badge.url : string.Empty; } }

        public string MixerChannelBadgeLink { get { return this.FanProgression?.level?.SmallAssetURL?.ToString(); } }

        public bool HasMixerChannelBadgeLink { get { return !string.IsNullOrEmpty(this.MixerChannelBadgeLink); } }

        private readonly HashSet<MixerRoleEnum> mixerRoles = new HashSet<MixerRoleEnum>();
        public HashSet<MixerRoleEnum> MixerRoles
        {
            get
            {
                lock (this.mixerRoles)
                {
                    if (this.FollowDate != null && this.FollowDate.GetValueOrDefault() > DateTimeOffset.MinValue && !this.mixerRoles.Contains(MixerRoleEnum.Follower))
                    {
                        this.mixerRoles.Add(MixerRoleEnum.Follower);
                    }
                    return new HashSet<MixerRoleEnum>(mixerRoles);
                }
            }
        }

        [JsonIgnore]
        public DateTimeOffset LastActivity { get; set; }

        [JsonIgnore]
        public DateTimeOffset LastUpdated { get; set; }

        [JsonIgnore]
        public UserDataViewModel Data { get { return ChannelSession.Settings.UserData.GetValueIfExists(this.ID, new UserDataViewModel(this)); } }

        [JsonIgnore]
        public string RolesDisplayString { get; private set; }

        [JsonIgnore]
        public bool IsAnonymous { get { return this.ID == 0 || this.InteractiveIDs.Values.Any(i => i.anonymous.GetValueOrDefault()); } }

        [JsonIgnore]
        public MixerRoleEnum PrimaryRole { get { return this.MixerRoles.Max(); } }

        [JsonIgnore]
        public string PrimaryRoleString { get { return EnumHelper.GetEnumName(this.PrimaryRole); } }

        [JsonIgnore]
        public string SortableID
        {
            get
            {
                MixerRoleEnum role = this.PrimaryRole;
                if (role < MixerRoleEnum.Subscriber)
                {
                    role = MixerRoleEnum.User;
                }
                return (999 - role) + "-" + this.Platform.ToString() + "-" + this.UserName;
            }
        }

        [JsonIgnore]
        public string Title
        {
            get
            {
                if (!string.IsNullOrEmpty(this.Data.CustomTitle))
                {
                    return this.Data.CustomTitle;
                }

                UserTitleModel title = ChannelSession.Settings.UserTitles.OrderByDescending(t => t.Role).ThenByDescending(t => t.Months).FirstOrDefault(t => t.MeetsTitle(this));
                if (title != null)
                {
                    return title.Name;
                }

                return "No Title";
            }
            set
            {
                this.Data.CustomTitle = value;
            }
        }

        [JsonIgnore]
        public string MixerAgeString { get { return (this.MixerAccountDate != null) ? this.MixerAccountDate.GetValueOrDefault().GetAge() : "Unknown"; } }

        [JsonIgnore]
        public bool IsFollower { get { return this.MixerRoles.Contains(MixerRoleEnum.Follower) || this.HasPermissionsTo(MixerRoleEnum.Subscriber); } }

        [JsonIgnore]
        public string FollowAgeString { get { return (this.FollowDate != null) ? this.FollowDate.GetValueOrDefault().GetAge() : "Not Following"; } }

        [JsonIgnore]
        public bool IsMixerSubscriber { get { return this.MixerRoles.Contains(MixerRoleEnum.Subscriber); } }

        [JsonIgnore]
        public bool ShowMixerSubscriberBadge { get { return this.IsMixerSubscriber && !string.IsNullOrEmpty(this.SubscriberBadgeLink); } }

        [JsonIgnore]
        public string MixerSubscribeAgeString { get { return (this.MixerSubscribeDate != null) ? this.MixerSubscribeDate.GetValueOrDefault().GetAge() : "Not Subscribed"; } }

        [JsonIgnore]
        public int WhispererNumber { get; set; }

        [JsonIgnore]
        public bool HasWhisperNumber { get { return this.WhispererNumber > 0; } }

        [JsonIgnore]
        public int SubscribeMonths
        {
            get
            {
                if (this.MixerSubscribeDate != null)
                {
                    return this.MixerSubscribeDate.GetValueOrDefault().TotalMonthsFromNow();
                }
                return 0;
            }
        }

        [JsonIgnore]
        public string PrimaryRoleColorName
        {
            get
            {
                switch (this.PrimaryRole)
                {
                    case MixerRoleEnum.Streamer:
                        return "UserStreamerRoleColor";
                    case MixerRoleEnum.Staff:
                        return "UserStaffRoleColor";
                    case MixerRoleEnum.ChannelEditor:
                    case MixerRoleEnum.Mod:
                        return "UserModRoleColor";
                    case MixerRoleEnum.GlobalMod:
                        return "UserGlobalModRoleColor";
                }

                if (this.MixerRoles.Contains(MixerRoleEnum.Pro))
                {
                    return "UserProRoleColor";
                }
                else
                {
                    return "UserDefaultRoleColor";
                }
            }
        }

        [JsonIgnore]
        public bool IsInteractiveParticipant { get { return this.InteractiveIDs.Count > 0; } }

        [JsonIgnore]
        public PatreonTier PatreonTier
        {
            get
            {
                if (ChannelSession.Services.Patreon != null && this.PatreonUser != null)
                {
                    return ChannelSession.Services.Patreon.Campaign.GetTier(this.PatreonUser.TierID);
                }
                return null;
            }
        }

        public bool HasPermissionsTo(MixerRoleEnum checkRole)
        {
            if (checkRole == MixerRoleEnum.Subscriber && this.IsEquivalentToMixerSubscriber())
            {
                return true;
            }
            return this.PrimaryRole >= checkRole;
        }

        public bool ExceedsPermissions(MixerRoleEnum checkRole) { return this.PrimaryRole > checkRole; }

        public bool IsEquivalentToMixerSubscriber()
        {
            if (this.PatreonUser != null && ChannelSession.Services.Patreon != null && !string.IsNullOrEmpty(ChannelSession.Settings.PatreonTierMixerSubscriberEquivalent))
            {
                PatreonTier userTier = this.PatreonTier;
                PatreonTier equivalentTier = ChannelSession.Services.Patreon.Campaign.GetTier(ChannelSession.Settings.PatreonTierMixerSubscriberEquivalent);
                if (userTier != null && equivalentTier != null && userTier.Amount >= equivalentTier.Amount)
                {
                    return true;
                }
            }
            return false;
        }

        public void UpdateLastActivity() { this.LastActivity = DateTimeOffset.Now; }

        public async Task RefreshDetails(bool force = false)
        {
            if (!this.IsAnonymous && (this.LastUpdated.TotalMinutesFromNow() >= 1 || force))
            {
                UserWithChannelModel user = await ChannelSession.MixerStreamerConnection.GetUser(this.ID);
                if (user != null)
                {
                    this.SetMixerUserDetails(user);

                    this.FollowDate = await ChannelSession.MixerStreamerConnection.CheckIfFollows(ChannelSession.MixerChannel, this.GetModel());
                    if (this.IsMixerSubscriber || force)
                    {
                        UserWithGroupsModel userGroups = await ChannelSession.MixerStreamerConnection.GetUserInChannel(ChannelSession.MixerChannel, this.ID);
                        if (userGroups != null)
                        {
                            this.MixerSubscribeDate = userGroups.GetSubscriberDate();
                            if (this.MixerSubscribeDate != null)
                            {
                                if (this.Data.TotalMonthsSubbed < this.MixerSubscribeDate.GetValueOrDefault().TotalMonthsFromNow())
                                {
                                    this.Data.TotalMonthsSubbed = (uint)this.MixerSubscribeDate.GetValueOrDefault().TotalMonthsFromNow();
                                }
                            }
                        }
                    }

                    this.FanProgression = await ChannelSession.MixerStreamerConnection.GetUserFanProgression(ChannelSession.MixerChannel, user);
                }

                if (!this.IsInChat)
                {
                    await this.RefreshChatDetails(addToChat: false);
                }

                await this.SetCustomRoles();

                this.LastUpdated = DateTimeOffset.Now;
            }
        }

        public async Task RefreshChatDetails(bool addToChat = true)
        {
            if (!this.IsAnonymous && this.LastUpdated.TotalMinutesFromNow() >= 1)
            {
                ChatUserModel chatUser = await ChannelSession.MixerStreamerConnection.GetChatUser(ChannelSession.MixerChannel, this.ID);
                if (chatUser != null)
                {
                    this.SetChatDetails(chatUser, addToChat);
                }
            }
        }

        public async Task SetCustomRoles()
        {
            if (!this.IsAnonymous)
            {
                if (ChannelSession.Services.Patreon != null)
                {
                    if (this.PatreonUser == null)
                    {
                        await this.SetPatreonSubscriber();
                    }
                }
            }
        }

        public void SetChatDetails(ChatUserModel chatUser, bool addToChat = true)
        {
            if (chatUser != null)
            {
                this.SetMixerRoles(chatUser.userRoles);
                if (addToChat)
                {
                    this.IsInChat = true;
                }
            }
        }

        public void RemoveChatDetails(ChatUserModel chatUser)
        {
            this.IsInChat = false;
        }

        public void SetInteractiveDetails(MixPlayParticipantModel participant)
        {
            this.InteractiveIDs[participant.sessionID] = participant;
            this.InteractiveGroupID = participant.groupID;
        }

        public void RemoveInteractiveDetails(MixPlayParticipantModel participant)
        {
            this.InteractiveIDs.Remove(participant.sessionID);
            if (this.InteractiveIDs.Count == 0)
            {
                this.InteractiveGroupID = MixPlayUserGroupModel.DefaultName;
            }
        }

        public Task SetPatreonSubscriber()
        {
            if (ChannelSession.Services.Patreon != null)
            {
                IEnumerable<PatreonCampaignMember> campaignMembers = ChannelSession.Services.Patreon.CampaignMembers;

                PatreonCampaignMember patreonUser = null;
                if (!string.IsNullOrEmpty(this.Data.PatreonUserID))
                {
                    patreonUser = campaignMembers.FirstOrDefault(u => u.UserID.Equals(this.Data.PatreonUserID));
                }
                else
                {
                    patreonUser = campaignMembers.FirstOrDefault(u => u.User.LookupName.Equals(this.UserName, StringComparison.CurrentCultureIgnoreCase));
                }

                this.PatreonUser = patreonUser;
                if (patreonUser != null)
                {
                    this.Data.PatreonUserID = patreonUser.UserID;
                }
                else
                {
                    this.Data.PatreonUserID = null;
                }
            }
            return Task.FromResult(0);
        }

        public async Task AddModerationStrike(string moderationReason = null)
        {
            Dictionary<string, string> extraSpecialIdentifiers = new Dictionary<string, string>();
            extraSpecialIdentifiers.Add(ModerationHelper.ModerationReasonSpecialIdentifier, moderationReason);

            this.Data.ModerationStrikes++;
            if (this.Data.ModerationStrikes == 1)
            {
                if (ChannelSession.Settings.ModerationStrike1Command != null)
                {
                    await ChannelSession.Settings.ModerationStrike1Command.Perform(this, extraSpecialIdentifiers: extraSpecialIdentifiers);
                }
            }
            else if (this.Data.ModerationStrikes == 2)
            {
                if (ChannelSession.Settings.ModerationStrike2Command != null)
                {
                    await ChannelSession.Settings.ModerationStrike2Command.Perform(this, extraSpecialIdentifiers: extraSpecialIdentifiers);
                }
            }
            else if (this.Data.ModerationStrikes >= 3)
            {
                if (ChannelSession.Settings.ModerationStrike3Command != null)
                {
                    await ChannelSession.Settings.ModerationStrike3Command.Perform(this, extraSpecialIdentifiers: extraSpecialIdentifiers);
                }
            }
        }

        public Task RemoveModerationStrike()
        {
            if (this.Data.ModerationStrikes > 0)
            {
                this.Data.ModerationStrikes--;
            }
            return Task.FromResult(0);
        }

        public void UpdateMinuteData()
        {
            if (ChannelSession.MixerChannel.online)
            {
                this.Data.ViewingMinutes++;
            }
            else
            {
                this.Data.OfflineViewingMinutes++;
            }
            ChannelSession.Settings.UserData.ManualValueChanged(this.ID);

            if (ChannelSession.Settings.RegularUserMinimumHours > 0 && this.Data.ViewingHoursPart >= ChannelSession.Settings.RegularUserMinimumHours)
            {
                this.mixerRoles.Add(MixerRoleEnum.Regular);
            }
        }

        public UserModel GetModel()
        {
            return new UserModel()
            {
                id = this.ID,
                username = this.UserName,
            };
        }

        public ChatUserModel GetChatModel()
        {
            return new ChatUserModel()
            {
                userId = this.ID,
                userName = this.UserName,
                userRoles = this.MixerRoles.Select(r => r.ToString()).ToArray(),
            };
        }

        public IEnumerable<MixPlayParticipantModel> GetParticipantModels()
        {
            List<MixPlayParticipantModel> participants = new List<MixPlayParticipantModel>();
            foreach (string interactiveID in this.InteractiveIDs.Keys)
            {
                participants.Add(new MixPlayParticipantModel()
                {
                    userID = this.ID,
                    username = this.UserName,
                    sessionID = interactiveID,
                    groupID = this.InteractiveGroupID,
                    disabled = this.IsInInteractiveTimeout,
                });
            }
            return participants;
        }

        public override bool Equals(object obj)
        {
            if (obj is UserViewModel)
            {
                return this.Equals((UserViewModel)obj);
            }
            return false;
        }

        public bool Equals(UserViewModel other) { return this.ID.Equals(other.ID); }

        public int CompareTo(UserViewModel other) { return this.UserName.CompareTo(other.UserName); }

        public override int GetHashCode() { return this.ID.GetHashCode(); }

        public override string ToString() { return this.UserName; }

        private void SetMixerUserDetails(UserModel user)
        {
            this.MixerAccountDate = user.createdAt;
            this.Sparks = (int)user.sparks;
            this.TwitterURL = user.social?.twitter;
            if (user is UserWithChannelModel)
            {
                UserWithChannelModel userChannel = (UserWithChannelModel)user;
                this.ChannelID = userChannel.channel.id;
                this.CurrentViewerCount = userChannel.channel.viewersCurrent;
            }
        }

        private void SetMixerRoles(string[] userRoles)
        {
            lock (this.mixerRoles)
            {
                this.mixerRoles.Clear();
                this.mixerRoles.Add(MixerRoleEnum.User);

                if (userRoles != null && userRoles.Length > 0)
                {
                    if (userRoles.Any(r => r.Equals("Owner"))) { this.mixerRoles.Add(MixerRoleEnum.Streamer); }
                    if (userRoles.Any(r => r.Equals("Staff"))) { this.mixerRoles.Add(MixerRoleEnum.Staff); }
                    if (userRoles.Any(r => r.Equals("ChannelEditor"))) { this.mixerRoles.Add(MixerRoleEnum.ChannelEditor); }
                    if (userRoles.Any(r => r.Equals("Mod"))) { this.mixerRoles.Add(MixerRoleEnum.Mod); }
                    if (userRoles.Any(r => r.Equals("GlobalMod"))) { this.mixerRoles.Add(MixerRoleEnum.GlobalMod); }
                    if (userRoles.Any(r => r.Equals("Subscriber"))) { this.mixerRoles.Add(MixerRoleEnum.Subscriber); }
                    if (userRoles.Any(r => r.Equals("Partner"))) { this.mixerRoles.Add(MixerRoleEnum.Partner); }
                    if (userRoles.Any(r => r.Equals("Pro"))) { this.mixerRoles.Add(MixerRoleEnum.Pro); }
                    if (userRoles.Any(r => r.Equals("Banned"))) { this.mixerRoles.Add(MixerRoleEnum.Banned); }
                }

                if (ChannelSession.MixerChannel != null && ChannelSession.MixerChannel.user.id.Equals(this.ID))
                {
                    this.mixerRoles.Add(MixerRoleEnum.Streamer);
                }

                if (this.mixerRoles.Contains(MixerRoleEnum.Streamer))
                {
                    this.mixerRoles.Add(MixerRoleEnum.ChannelEditor);
                    this.mixerRoles.Add(MixerRoleEnum.Subscriber);
                    this.mixerRoles.Add(MixerRoleEnum.Follower);
                }
                
                if (this.mixerRoles.Contains(MixerRoleEnum.ChannelEditor))
                {
                    this.mixerRoles.Add(MixerRoleEnum.Mod);
                }

                if (ChannelSession.Settings.RegularUserMinimumHours > 0 && this.Data.ViewingHoursPart >= ChannelSession.Settings.RegularUserMinimumHours)
                {
                    this.mixerRoles.Add(MixerRoleEnum.Regular);
                }

                List<MixerRoleEnum> mixerDisplayRoles = this.mixerRoles.ToList();
                if (this.mixerRoles.Contains(MixerRoleEnum.Banned))
                {
                    mixerDisplayRoles.Clear();
                    mixerDisplayRoles.Add(MixerRoleEnum.Banned);
                }
                else
                {
                    if (this.mixerRoles.Count() > 1)
                    {
                        mixerDisplayRoles.Remove(MixerRoleEnum.User);
                    }

                    if (mixerDisplayRoles.Contains(MixerRoleEnum.ChannelEditor))
                    {
                        mixerDisplayRoles.Remove(MixerRoleEnum.Mod);
                    }

                    if (this.mixerRoles.Contains(MixerRoleEnum.Subscriber) || this.mixerRoles.Contains(MixerRoleEnum.Streamer))
                    {
                        mixerDisplayRoles.Remove(MixerRoleEnum.Follower);
                    }

                    if (this.mixerRoles.Contains(MixerRoleEnum.Streamer))
                    {
                        mixerDisplayRoles.Remove(MixerRoleEnum.ChannelEditor);
                        mixerDisplayRoles.Remove(MixerRoleEnum.Subscriber);
                    }
                }

                List<string> displayRoles = new List<string>(mixerDisplayRoles.Select(r => EnumLocalizationHelper.GetLocalizedName(r)));
                this.RolesDisplayString = string.Join(", ", displayRoles.OrderByDescending(r => r));
            }
        }
    }
}
