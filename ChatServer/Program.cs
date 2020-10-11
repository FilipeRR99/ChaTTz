using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatServer.Services;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel;

namespace ChatServer
{
    public class RegisteredUser
    {
        private string Name;
        private int Id;
        private string HashedPassword;

        public RegisteredUser(string name, int id, string hashedPassword)
        {
            Name = name;
            Id = id;
            HashedPassword = hashedPassword;
        }

        public string GetName() { return this.Name; }
        public string GetHashedPassword() { return this.HashedPassword; }
        public int GetId() { return this.Id; }
    }
    public class ServerManagement
    {
        private List<RegisteredUser> Users;
        private Dictionary<RegisteredUser, String> LoggedUsers;
        private List<Message> PendingMessages;
        private int LastId = 1;

        public ServerManagement() {
            Users = new List<RegisteredUser>();
            LoggedUsers = new Dictionary<RegisteredUser, string>();
            PendingMessages = new List<Message>();
            LastId = 1;
        }

        public User CreateUser(User user, string password)
        {
            string hashedPassword = this.HashPassword(password);

            int computedId = this.LastId + 1;
            this.LastId = computedId;

            RegisteredUser registered = new RegisteredUser(user.Name, computedId, hashedPassword);
            this.Users.Add(registered); // add new user

            User newUser = new User();
            user.Id = registered.GetId();
            user.Name = registered.GetName();

            return newUser;
        }

        public bool Login(User user, string password, string url)
        {
            string hashedPassword = HashPassword(password);
            RegisteredUser correspondentUser = Users.Find(item => item.GetId() == user.Id 
            && item.GetHashedPassword() == hashedPassword
            && item.GetName() == user.Name);

            if (correspondentUser != null)
            {
                LoggedUsers.Add(correspondentUser, url);

                return true;
            }
            else return false;
        }

        public List<User> GetUsers(User user, string url)
        {
            if (IsLoggedIn(user, url))
            {
                List<User> users = new List<User>();
                
                foreach(RegisteredUser registeredUser in Users)
                {
                    User tempUser = new User
                    {
                        Id = registeredUser.GetId(),
                        Name = registeredUser.GetName()
                    };
                    users.Add(tempUser);
                }

                return users;
            } else
            {
                return new List<User>();
            }
        }

        public MessageStatus SendMessage(Message message, User user, string url)
        {
            if (IsLoggedIn(user, url)) // if sender has permission to send (is logged in with that url)
            {
                if (message.Sender.Equals(user)) // check that message is consistent
                {
                    PendingMessages.Add(message);
                    return MessageStatus.MessageSent;
                }
                else return MessageStatus.MessageFailed;
            } else 
                return MessageStatus.MessageFailed;
        }

        public bool Logout(User user, string url)
        {
            return IsLoggedIn(user, url) && RemovedFromLoggedUsers(user);
        }
        public List<Message> GetMessageCorrespondence(User user, string url)
        {
            List<Message> userMessages = new List<Message>();

            if (IsLoggedIn(user, url))
            {
                foreach (Message message in PendingMessages)
                {
                    if (message.Receiver.Id == user.Id) // if receiver has same id, since ids are unique
                    {
                        userMessages.Add(message);
                    }
                }
            }
            return userMessages;
        }

        // Auxiliary methods
        public string HashPassword(string password)
        {
            var sha1 = new SHA1CryptoServiceProvider();
            var data = Encoding.ASCII.GetBytes(password);
            var sha1data = sha1.ComputeHash(data);
            return ASCIIEncoding.ASCII.GetString(sha1data);
        }

        public bool IsLoggedIn(User user, string url)
        {
            RegisteredUser registeredUser = GetRegisteredUser(user);

            return LoggedUsers.ContainsKey(registeredUser) && LoggedUsers.GetValueOrDefault(registeredUser) == url;
        }

        public bool RemovedFromLoggedUsers(User user) {
            RegisteredUser registered = GetRegisteredUser(user);
            return LoggedUsers.Remove(registered); // true if removed
        }

        public RegisteredUser GetRegisteredUser(User user)
        {
            return Users.Find(item => item.GetId() == user.Id && item.GetName() == user.Name);
        }
    }

    class Program
    {
        const int Port = 5656;
        
        static void Main(string[] args)
        {
            ServerManagement serverManagement = new ServerManagement();
            ServerService serverService = new ServerService(serverManagement);
            
            Server server = new Server
            {
                Services = { ChatServerService.BindService(serverService) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.WriteLine("Chat server listening on port " + Port);
            Console.WriteLine("Press any key to stop the server");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}

