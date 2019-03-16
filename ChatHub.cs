using Microsoft.AspNetCore.SignalR;
using MyApp.Models.Domain;
using MyApp.Models.Requests;
using MyApp.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyApp.Web.Api.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;
        private readonly IAuthenticationService<int> _authService;

        public ChatHub(
            IMessageService messageService,
            IAuthenticationService<int> authService)
        {
            _messageService = messageService;
            _authService = authService;
        }

        private static Dictionary<int, List<string>> Users = new Dictionary<int, List<string>>();

        public override Task OnConnectedAsync()
        {
            int currentUserId = _authService.GetCurrentUserId();
            string connectionID = this.Context.ConnectionId;

            if (!Users.ContainsKey(currentUserId))
            {
                Users[currentUserId] = new List<string>();
            }
            Users[currentUserId].Add(connectionID);

            return base.OnConnectedAsync();

        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            int currentUserId = _authService.GetCurrentUserId();
            string connectionID = Context.ConnectionId;

            if (Users.ContainsKey(currentUserId))
            {
                Users.Remove(currentUserId);
            }

            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(MessageAddRequest msgAdd)
        {
            int userId = _authService.GetCurrentUserId();

            msgAdd.ConversationId = msgAdd.ConversationId;
            msgAdd.Body = msgAdd.Body;
            msgAdd.RecepientId = msgAdd.RecepientId;
            msgAdd.UserId = userId;

            int msgId = _messageService.Create(msgAdd, userId);

            Message msgInfo = new Message();
            msgInfo = _messageService.Get(msgId);

            MessageRecepientAddRequest msgRecAdd = new MessageRecepientAddRequest();
            msgRecAdd.ConversationId = msgAdd.ConversationId;
            msgRecAdd.MessageId = msgId;
            msgRecAdd.TargetUserId = msgAdd.RecepientId;
            msgRecAdd.Read = false;

            _messageService.SendMsgToRecepient(msgRecAdd);

            if (Users.ContainsKey(msgAdd.RecepientId))
            {
                foreach (string connectionID in Users[msgAdd.RecepientId])
                {
                    await Clients.Client(connectionID).SendAsync("ReceiveMessageNoti", msgInfo);
                    await Clients.Client(connectionID).SendAsync("ReceiveMessage", msgInfo);
                }

                //await Clients.Clients(connectIds).SendAsync("ReceiveMessageNoti", msgInfo);
                //await Clients.Clients(connectIds).SendAsync("ReceiveMessage", msgInfo);

            }

            await Clients.Caller.SendAsync("ReceiveMessage", msgInfo);
        }

        public async Task SearchUserProfilesToAdd(string search)
        {
            List<UserProfile> returnMatchedUserProfiles = _messageService.SearchUserProfilesToAdd(search);

            await Clients.Caller.SendAsync("ReturnUserProfiles", returnMatchedUserProfiles);
        }

        public async Task AddConversation(int recepientUserId)
        {
            int senderUserId = _authService.GetCurrentUserId();

            UserProfile selectedUserToAdd = _messageService.SelectUserToAdd(recepientUserId);
            UserProfile senderUserToAdd = _messageService.SelectUserToAdd(senderUserId);

            ConversationAddRequest newConversation = new ConversationAddRequest();
            newConversation.Name = Guid.NewGuid();

            int conversationId = _messageService.CreateConversation(newConversation.Name, senderUserId);

            ConversationParticipantAddRequest senderAddReq = new ConversationParticipantAddRequest();
            senderAddReq.ConversationId = conversationId;
            senderAddReq.UserId = senderUserId;

            ConversationParticipantAddRequest recepientAddReq = new ConversationParticipantAddRequest();
            recepientAddReq.ConversationId = conversationId;
            recepientAddReq.UserId = recepientUserId;

            _messageService.TwoWayAddConversationParticipants(senderAddReq, recepientAddReq);

            var newConvoWithParticipants = new
            {
                ConversationId = conversationId,
                participantId = recepientUserId,
                firstName = selectedUserToAdd.FirstName,
                lastName = selectedUserToAdd.LastName,
                avatarUrl = selectedUserToAdd.AvatarUrl
            };

            if (Users.ContainsKey(recepientAddReq.UserId))
            {
                foreach (string connectionID in Users[recepientAddReq.UserId])
                {
                    await Clients.Client(connectionID).SendAsync("NewConversation", newConvoWithParticipants);
                }
            }

            await Clients.Caller.SendAsync("NewConversation", newConvoWithParticipants);
        }

        public async Task GetConversationList()
        {
            int userId = _authService.GetCurrentUserId();

            List<ConversationParticipant> threads = _messageService.GetConversationThreads(userId);
            await Clients.Caller.SendAsync("LoadConversations", threads);
        }

        public async Task LoadCurrentConversation(int conversationId)
        {
            List<Message> currentConversation = _messageService.GetMessagesByConversationId(conversationId);
            await Clients.Caller.SendAsync("GetCurrentConversation", currentConversation);
        }

        public async Task DeleteConversationsAndMessages(int conversationId)
        {
            _messageService.DeleteConversationsAndMessages(conversationId);
            await Clients.All.SendAsync("DeleteSelectedConversation");
        }

        public async Task GetCurrentUserId()
        {
            int currentUserId = _authService.GetCurrentUserId();

            await Clients.Caller.SendAsync("ReturnCurrentUserId", currentUserId);
        }

    }
}
