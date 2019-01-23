﻿using MixItUp.API.Models;
using MixItUp.Base;
using MixItUp.Base.ViewModel.User;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web.Http;

namespace MixItUp.Desktop.Services.DeveloperAPI
{
    [RoutePrefix("api/mixplay")]
    public class MixPlayController : ApiController
    {
        [Route("users")]
        [HttpGet]
        public async Task<IEnumerable<MixPlayUser>> GetUsers()
        {
            var mixplayUsers = await ChannelSession.ActiveUsers.GetAllWorkableUsers();
            return mixplayUsers.Where(x => x.IsInteractiveParticipant).Select(x => new MixPlayUser()
            {
                ID = x.ID,
                UserName = x.UserName,
                ParticipantIDs = x.InteractiveIDs.Keys.ToList(),
            });
        }

        [Route("user/{userID}")]
        [HttpGet]
        public async Task<MixPlayUser> GetUser(uint userID)
        {
            UserViewModel user = await ChannelSession.ActiveUsers.GetUserByID(userID);
            if (user == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new ObjectContent<Error>(new Error { Message = $"Unable to find user by ID: {userID}." }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "User not found"
                };
                throw new HttpResponseException(resp);
            }

            return new MixPlayUser()
            {
                ID = user.ID,
                UserName = user.UserName,
                ParticipantIDs = user.InteractiveIDs.Keys.ToList(),
            };
        }

        [Route("user/search/{userName}")]
        [HttpGet]
        public async Task<MixPlayUser> GetUserByUserName(string userName)
        {
            UserViewModel user = await ChannelSession.ActiveUsers.GetUserByUsername(userName);

            if (user == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new ObjectContent<Error>(new Error { Message = $"Unable to find user by name: {userName}." }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "User not found"
                };
                throw new HttpResponseException(resp);
            }

            return new MixPlayUser()
            {
                ID = user.ID,
                UserName = user.UserName,
                ParticipantIDs = user.InteractiveIDs.Keys.ToList(),
            };
        }

        [Route("broadcast")]
        [HttpPost]
        public async Task Broadcast([FromBody] JObject data, [FromBody] MixPlayBroadcastTargetBase[] targets)
        {
            if (data == null || targets == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new ObjectContent<Error>(new Error { Message = "Unable to parse broadcast from POST body." }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "Invalid POST Body"
                };
                throw new HttpResponseException(resp);
            }

            if (!ChannelSession.Interactive.IsConnected())
            {
                var resp = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new ObjectContent<Error>(new Error { Message = "Unable to broadcast because to MixPlay is not connected" }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "MixPlay Service Not Connected"
                };
                throw new HttpResponseException(resp);
            }

            await ChannelSession.Interactive.BroadcastEvent(targets.Select(x => x.ScopeString()).ToList(), data);
        }

        [Route("broadcast/users")]
        [HttpPost]
        public async Task Broadcast([FromBody] JObject data, [FromBody] MixPlayBroadcastUser[] users)
        {
            if (data == null || users == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new ObjectContent<Error>(new Error { Message = "Unable to parse broadcast from POST body." }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "Invalid POST Body"
                };
                throw new HttpResponseException(resp);
            }

            if (!ChannelSession.Interactive.IsConnected())
            {
                var resp = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new ObjectContent<Error>(new Error { Message = "Unable to broadcast because to MixPlay is not connected" }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "MixPlay Service Not Connected"
                };
                throw new HttpResponseException(resp);
            }

            MixPlayBroadcastParticipant[] targets;

            var mixplayUsers = await ChannelSession.ActiveUsers.GetUsersByID(users.Select(x => x.UserID).ToArray());

            targets = mixplayUsers.Where(x => x.IsInteractiveParticipant).SelectMany(x => x.InteractiveIDs.Keys).Select(x => new MixPlayBroadcastParticipant(x)).ToArray();

            if (targets == null || targets.Count() == 0)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new ObjectContent<Error>(new Error { Message = "No Matching Users Found For The Provided IDs" }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "No Users Found"
                };
                throw new HttpResponseException(resp);
            }
            await Broadcast(data, targets);
        }
    }
}