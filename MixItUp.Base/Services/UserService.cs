﻿using Mixer.Base.Model.Chat;
using Mixer.Base.Model.MixPlay;
using Mixer.Base.Model.User;
using MixItUp.Base.Model;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MixItUp.Base.Services
{
    public static class ChatUserModelExtensions
    {
        public static ChatUserModel ToChatUserModel(this ChatUserEventModel chatUser) { return new ChatUserModel() { userId = chatUser.id, userName = chatUser.username, userRoles = chatUser.roles }; }

        public static ChatUserModel ToChatUserModel(this ChatMessageUserModel chatMessageUser) { return new ChatUserModel() { userId = chatMessageUser.user_id, userName = chatMessageUser.user_name, userRoles = chatMessageUser.user_roles }; }
    }

    public interface IUserService
    {
        UserViewModel GetUserByUsername(string username);

        UserViewModel GetUserByMixerID(uint id);

        UserViewModel GetUserByMixPlayID(string id);

        IEnumerable<UserViewModel> GetUsersByMixerID(IEnumerable<uint> ids);

        Task<UserViewModel> AddOrUpdateUser(UserModel userModel);

        Task<UserViewModel> AddOrUpdateUser(ChatUserEventModel chatUser);

        Task<UserViewModel> AddOrUpdateUser(ChatUserModel chatUser);

        Task<UserViewModel> AddOrUpdateUser(MixPlayParticipantModel mixplayUser);

        Task<UserViewModel> RemoveUser(ChatUserEventModel chatUser);

        Task<UserViewModel> RemoveUser(ChatUserModel chatUser);

        Task<UserViewModel> RemoveUser(MixPlayParticipantModel mixplayUser);

        void Clear();

        IEnumerable<UserViewModel> GetAllUsers();

        IEnumerable<UserViewModel> GetAllWorkableUsers();

        int Count();
    }

    public class UserService : IUserService
    {
        public static readonly HashSet<string> SpecialUserAccounts = new HashSet<string>() { "HypeBot", "boomtvmod", "StreamJar", "PretzelRocks", "ScottyBot", "Streamlabs", "StreamElements" };

        private LockedDictionary<Guid, UserViewModel> usersByID = new LockedDictionary<Guid, UserViewModel>();

        private LockedDictionary<uint, UserViewModel> usersByMixerID = new LockedDictionary<uint, UserViewModel>();
        private LockedDictionary<string, UserViewModel> usersByMixerUsername = new LockedDictionary<string, UserViewModel>();
        private LockedDictionary<string, UserViewModel> usersByMixPlayID = new LockedDictionary<string, UserViewModel>();

        public UserViewModel GetUserByUsername(string username)
        {
            UserViewModel user = null;
            if (this.usersByMixerUsername.TryGetValue(username.ToLower(), out user))
            {
                return user;
            }
            return null;
        }

        public UserViewModel GetUserByMixerID(uint id)
        {
            if (this.usersByMixerID.TryGetValue(id, out UserViewModel user))
            {
                return user;
            }
            return null;
        }

        public UserViewModel GetUserByMixPlayID(string id)
        {
            if (this.usersByMixPlayID.TryGetValue(id, out UserViewModel user))
            {
                return user;
            }
            return null;
        }

        public IEnumerable<UserViewModel> GetUsersByMixerID(IEnumerable<uint> ids)
        {
            List<UserViewModel> results = new List<UserViewModel>();
            foreach (uint id in ids)
            {
                if (this.usersByMixerID.TryGetValue(id, out UserViewModel user))
                {
                    results.Add(user);
                }
            }
            return results;
        }

        public async Task<UserViewModel> AddOrUpdateUser(UserModel userModel)
        {
            UserViewModel user = new UserViewModel(userModel);
            if (user.MixerID > 0)
            {
                if (this.usersByMixerID.ContainsKey(user.MixerID))
                {
                    user = this.usersByMixerID[user.MixerID];
                }
                await this.AddOrUpdateUser(user);
            }
            return user;
        }

        public async Task<UserViewModel> AddOrUpdateUser(ChatUserEventModel chatUser) { return await this.AddOrUpdateUser(chatUser.ToChatUserModel()); }

        public async Task<UserViewModel> AddOrUpdateUser(ChatUserModel chatUser)
        {
            UserViewModel user = new UserViewModel(chatUser);
            if (chatUser.userId.HasValue && chatUser.userId.GetValueOrDefault() > 0)
            {
                if (this.usersByMixerID.ContainsKey(chatUser.userId.GetValueOrDefault()))
                {
                    user = this.usersByMixerID[chatUser.userId.GetValueOrDefault()];
                }
                user.SetChatDetails(chatUser);
                await this.AddOrUpdateUser(user);
            }
            return user;
        }

        public async Task<UserViewModel> AddOrUpdateUser(MixPlayParticipantModel mixplayUser)
        {
            UserViewModel user = new UserViewModel(mixplayUser);
            if (mixplayUser.userID > 0 && !string.IsNullOrEmpty(mixplayUser.sessionID))
            {
                if (this.usersByMixerID.ContainsKey(mixplayUser.userID))
                {
                    user = this.usersByMixerID[mixplayUser.userID];
                }
                user.SetInteractiveDetails(mixplayUser);
                this.usersByMixPlayID[mixplayUser.sessionID] = user;
                await this.AddOrUpdateUser(user);
            }
            return user;
        }

        private async Task AddOrUpdateUser(UserViewModel user)
        {
            if (!user.IsAnonymous)
            {
                this.usersByID[user.ID] = user;

                if (user.MixerID > 0 && !string.IsNullOrEmpty(user.MixerUsername))
                {
                    this.usersByMixerID[user.MixerID] = user;
                    this.usersByMixerUsername[user.MixerUsername.ToLower()] = user;
                }

                if (UserService.SpecialUserAccounts.Contains(user.Username))
                {
                    user.IgnoreForQueries = true;
                }
                else
                {
                    user.IgnoreForQueries = false;
                    if (user.Data.ViewingMinutes == 0)
                    {
                        await ChannelSession.Services.Events.PerformEvent(new EventTrigger(EventTypeEnum.ChatUserFirstJoin, user));
                    }

                    if (ChannelSession.Services.Events.CanPerformEvent(new EventTrigger(EventTypeEnum.ChatUserJoined, user)))
                    {
                        user.Data.TotalStreamsWatched++;
                        await ChannelSession.Services.Events.PerformEvent(new EventTrigger(EventTypeEnum.ChatUserJoined, user));
                    }
                }
            }
        }

        public async Task<UserViewModel> RemoveUser(ChatUserEventModel chatUser) { return await this.RemoveUser(chatUser.ToChatUserModel()); }

        public async Task<UserViewModel> RemoveUser(ChatUserModel chatUser)
        {
            if (this.usersByMixerID.TryGetValue(chatUser.userId.GetValueOrDefault(), out UserViewModel user))
            {
                user.RemoveChatDetails(chatUser);
                if (user.InteractiveIDs.Count == 0)
                {
                    await this.RemoveUser(user);
                }
                return user;
            }
            return null;
        }

        public async Task<UserViewModel> RemoveUser(MixPlayParticipantModel mixplayUser)
        {
            if (this.usersByMixPlayID.TryGetValue(mixplayUser.sessionID, out UserViewModel user))
            {
                this.usersByMixPlayID.Remove(mixplayUser.sessionID);
                user.RemoveInteractiveDetails(mixplayUser);
                if (user.InteractiveIDs.Count == 0 && !user.IsInChat)
                {
                    await this.RemoveUser(user);
                }
                return user;
            }
            return null;
        }

        private async Task RemoveUser(UserViewModel user)
        {
            this.usersByID.Remove(user.ID);

            if (user.MixerID > 0)
            {
                this.usersByMixerID.Remove(user.MixerID);
                this.usersByMixerUsername.Remove(user.MixerUsername);
            }

            await ChannelSession.Services.Events.PerformEvent(new EventTrigger(EventTypeEnum.ChatUserLeft, user));
        }

        public void Clear()
        {
            this.usersByID.Clear();

            this.usersByMixerID.Clear();
            this.usersByMixerUsername.Clear();
            this.usersByMixPlayID.Clear();
        }

        public IEnumerable<UserViewModel> GetAllUsers() { return this.usersByID.Values; }

        public IEnumerable<UserViewModel> GetAllWorkableUsers()
        {
            IEnumerable<UserViewModel> results = this.GetAllUsers();
            return results.Where(u => !u.IgnoreForQueries);
        }

        public int Count() { return this.usersByID.Count; }
    }
}
