﻿using MixItUp.Base.Commands;
using MixItUp.Base.Model;
using MixItUp.Base.Services;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using MixItUp.Base.ViewModels;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MixItUp.Base.ViewModel.Chat
{
    public class ChatMessageViewModel : ViewModelBase, IEquatable<ChatMessageViewModel>
    {
        private const string TaggingRegexFormat = "(^|\\s+)@{0}(\\s+|$)";

        public string ID { get; private set; }

        public StreamingPlatformTypeEnum Platform { get; private set; }

        public List<object> MessageParts { get; protected set; } = new List<object>();

        public string PlainTextMessage { get; protected set; } = string.Empty;

        public string TargetUsername { get; protected set; }

        public bool IsInUsersChannel { get; protected set; } = true;

        public bool ContainsLink { get; protected set; } = false;

        public DateTimeOffset Timestamp { get; protected set; } = DateTimeOffset.Now;

        public bool IsDeleted { get; private set; }

        public string DeletedBy { get; private set; }

        public string ModerationReason { get; private set; }

        public UserViewModel User { get; set; }

        public event EventHandler OnDeleted = delegate { };

        public ChatMessageViewModel(string id, StreamingPlatformTypeEnum platform, UserViewModel user)
        {
            this.ID = id;
            this.Platform = platform;
            this.User = user;
        }

        public bool IsWhisper { get { return !string.IsNullOrEmpty(this.TargetUsername); } }

        public bool IsUserTagged { get { return Regex.IsMatch(this.PlainTextMessage, string.Format(TaggingRegexFormat, ChannelSession.MixerUser.username)); } }

        public bool IsStreamerOrBot { get { return this.User != null && this.User.MixerID.Equals(ChannelSession.MixerUser.id) || (ChannelSession.MixerBot != null && this.User.MixerID.Equals(ChannelSession.MixerBot.id)); } }

        public bool ShowTimestamp { get { return ChannelSession.Settings.ShowChatMessageTimestamps; } }

        public string TimestampDisplay { get { return string.Format("({0})", this.Timestamp.ToString("t")); } }

        public int FontSize { get { return ChannelSession.Settings.ChatFontSize; } }

        public string PrimaryTaggedUsername
        {
            get
            {
                if (this.PlainTextMessage.StartsWith("@"))
                {
                    int endIndex = this.PlainTextMessage.IndexOf(' ');
                    if (endIndex > 0)
                    {
                        return this.PlainTextMessage.Substring(1, endIndex - 1);
                    }
                    return this.PlainTextMessage.Substring(1);
                }
                return null;
            }
        }

        public virtual bool ContainsOnlyEmotes() { return false; }

        public async Task<bool> CheckForModeration()
        {
            if (!ModerationHelper.MeetsChatInteractiveParticipationRequirement(this.User, this))
            {
                Logger.Log(LogLevel.Debug, string.Format("Deleting Message As User does not meet requirement - {0} - {1}", ChannelSession.Settings.ModerationChatInteractiveParticipation, this.PlainTextMessage));
                await this.Delete(reason: "Chat/MixPlay Participation");
                await ModerationHelper.SendChatInteractiveParticipationWhisper(this.User, isChat: true);
                return true;
            }

            string moderationReason = await ModerationHelper.ShouldBeModerated(this.User, this.PlainTextMessage, this.ContainsLink);
            if (!string.IsNullOrEmpty(moderationReason))
            {
                Logger.Log(LogLevel.Debug, string.Format("Moderation Being Performed - {0}", this.ToString()));
                await this.Delete(reason: moderationReason);
                return true;
            }
            return false;
        }

        public async Task Delete(UserViewModel user = null, string reason = null)
        {
            if (!this.IsDeleted)
            {
                this.IsDeleted = true;
                if (user != null)
                {
                    this.DeletedBy = user.Username;
                }
                this.ModerationReason = reason;

                this.NotifyPropertyChanged("IsDeleted");
                this.NotifyPropertyChanged("DeletedBy");
                this.NotifyPropertyChanged("ModerationReason");

                this.OnDeleted(this, new EventArgs());

                if (this.User != null && !string.IsNullOrEmpty(this.PlainTextMessage))
                {
                    EventTrigger trigger = new EventTrigger(EventTypeEnum.ChatMessageDeleted, user);
                    trigger.SpecialIdentifiers["message"] = this.PlainTextMessage;
                    trigger.SpecialIdentifiers["reason"] = (!string.IsNullOrEmpty(this.ModerationReason)) ? this.ModerationReason : "Manual Deletion";
                    await ChannelSession.Services.Events.PerformEvent(trigger);
                }
            }
        }

        protected internal virtual void AddStringMessagePart(string str)
        {
            this.MessageParts.Add(str);
            if (string.IsNullOrEmpty(this.PlainTextMessage))
            {
                this.PlainTextMessage = str;
            }
            else
            {
                this.PlainTextMessage += " " + str;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is ChatMessageViewModel)
            {
                return this.Equals((ChatMessageViewModel)obj);
            }
            return false;
        }

        public bool Equals(ChatMessageViewModel other) { return this.ID.Equals(other.ID); }

        public override int GetHashCode() { return this.ID.GetHashCode(); }

        public override string ToString()
        {
            if (this.IsWhisper)
            {
                return string.Format("{0} -> {1}: {2}", this.User, this.TargetUsername, this.PlainTextMessage);
            }
            else
            {
                return string.Format("{0}: {1}", this.User, this.PlainTextMessage);
            }
        }
    }
}
