using Google.Protobuf.Collections;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChatServer.Services
{
    public class ServerService : ChatServerService.ChatServerServiceBase
    {
        private ServerManagement serverManagement;

        public ServerService()
        {
        }
        public ServerService(ServerManagement serverManagement)
        {
            this.serverManagement = serverManagement;
        }
        public override Task<ChatClientRegisterReply> Register(
            ChatClientRegisterRequest request, ServerCallContext context)
        {
            return Task.FromResult(Reg(request));
        }

        public ChatClientRegisterReply Reg(ChatClientRegisterRequest request)
        {
            User newUser;

            lock (this)
            {
                newUser = serverManagement.CreateUser(request.User, request.Password);
            }
            Console.WriteLine($"Registered client {newUser.Name} with Id {newUser.Id}");
            return new ChatClientRegisterReply
            {
                Ok = true,
                User = newUser
            };
        }

        public override Task<LoginReply> Login(
            LoginRequest request, ServerCallContext context)
        {
            return Task.FromResult(Logi(request));
        }

        public LoginReply Logi(LoginRequest request)
        {
            bool loggedIn = false;
            lock (this)
            {
                loggedIn = serverManagement.Login(request.User, request.Password, request.Url);
            }
            if (loggedIn)
            {
                Console.WriteLine($"Client {request.User.Name} with Id {request.User.Id} logged in at {request.Url}");
                return new LoginReply
                {
                   Ok = true
                }; 
            } else
            {
                Console.WriteLine($"Client {request.User.Name} with Id {request.User.Id} FAILED to log in at {request.Url}");
                return new LoginReply
                {
                    Ok = false
                };
            }
        }

        public override Task<GetUsersReply> GetUsers(
            GetUsersRequest request, ServerCallContext context)
        {
            return Task.FromResult(GetUsers(request));
        }

        public GetUsersReply GetUsers(GetUsersRequest request)
        {
            List<User> users = new List<User>();
            lock (this)
            {
                users = serverManagement.GetUsers(request.User, request.Url);
            }
            if (users.Count > 0)
            {
                Console.WriteLine($"Client {request.User.Name} with Id {request.User.Id} request all users at {request.Url}");
                GetUsersReply reply = new GetUsersReply();
                reply.Users.AddRange(users);

                return reply;
            }
            else
            {
                Console.WriteLine($"Client {request.User.Name} with Id {request.User.Id} FAILED to get all users at {request.Url}");
                GetUsersReply reply = new GetUsersReply();
                reply.Users.AddRange(users);

                return reply;
            }
        }

        public override Task<SendMessageReply> SendMessage(
            SendMessageRequest request, ServerCallContext context)
        {
            return Task.FromResult(SendMessage(request));
        }

        public SendMessageReply SendMessage(SendMessageRequest request)
        {
            MessageStatus messageStatus = MessageStatus.MessageFailed;

            lock (this)
            {
                messageStatus = serverManagement.SendMessage(request.Message, request.User, request.Url);
            }
            if (messageStatus.Equals(MessageStatus.MessageSent))
            {
                Console.WriteLine($"Client {request.User.Name} with Id {request.User.Id} sent a message at {request.Url}");
            }
            else
            {
                Console.WriteLine($"Client {request.User.Name} with Id {request.User.Id} FAILED to send a message at {request.Url}");
            }
            return new SendMessageReply
            {
                Status = messageStatus
            };
        }

        public override Task<LogOutReply> Logout(
            LogOutRequest request, ServerCallContext context)
        {
            return Task.FromResult(Logout(request));
        }

        public LogOutReply Logout(LogOutRequest request)
        {
            bool loggedOut = false;

            lock (this)
            {
                loggedOut = serverManagement.Logout(request.User, request.Url);
            }
            if (loggedOut)
            {
                Console.WriteLine($"Client {request.User.Name} with Id {request.User.Id} logged out at {request.Url}");
            }
            else
            {
                Console.WriteLine($"Client {request.User.Name} with Id {request.User.Id} FAILED to log out at {request.Url}");
            }
            return new LogOutReply
            {
                Ok = loggedOut
            };
        }

        public override Task<ReceiveMessageReply> ReceiveMessages(
           ReceiveMessageRequest request, ServerCallContext context)
        {
            return Task.FromResult(ReceiveMessages(request));
        }

        public ReceiveMessageReply ReceiveMessages(ReceiveMessageRequest request)
        {
            List<Message> messages = new List<Message>();
            lock (this)
            {
                messages = serverManagement.GetMessageCorrespondence(request.User, request.Url);
            }
            if (messages.Count > 0)
            {
                Console.WriteLine($"Client {request.User.Name} with Id {request.User.Id} request all messages at {request.Url}");
            }
            else
            {
                Console.WriteLine($"Client {request.User.Name} with Id {request.User.Id} FAILED to request all messages at {request.Url}");

            }
            ReceiveMessageReply reply = new ReceiveMessageReply();
            reply.Messages.AddRange(messages);

            return reply;
        }
    }
}
