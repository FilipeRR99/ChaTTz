using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Grpc.Core;
using Grpc.Net.Client;


namespace Clients
{

    public partial class Client 
    {
        private ServerService.ServerServiceClient client;
        static string serverPort="10001";
        string defaultServer = "http://localhost:"+serverPort;
        GrpcChannel channel;
        int myPort;
        string myURL;
        String[] lines;
        private int server_id;
        // ServerList = <serverId, URL>
        public Dictionary<int, string> ServerList = new Dictionary<int, string>();
        // DataCenter = <partionId, List<serverId>>
        public Dictionary<int, List<int>> DataCenter =new Dictionary<int, List<int>>();

        //primeiro master,ligo-te ao master da partition id
        public Client(String input_file)
        {

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            openChannel(defaultServer);
            Random rnd = new Random();
            // myPort = rnd.Next(5000, 50000);
            myPort = 1000;
            myURL = "http://localhost:" + myPort.ToString();
            string aux = @"{0}.txt";
            string path = aux.Replace("{0}", input_file);
            lines = System.IO.File.ReadAllLines("input_file.txt");
            server_id = getServerId();
            Dictionary<int, List<int>> tmp = new Dictionary<int, List<int>>();
            List<int> tmpA = new List<int>();
            List<int> tmpB = new List<int>();
            tmpA.Add(1);
            tmpB.Add(3);
            tmpA.Add(3);
            tmpB.Add(1);
            tmp.Add(1, tmpA);
            tmp.Add(2, tmpB);

            Dictionary<int, string> tmp2 = new Dictionary<int, string>();
            tmp2.Add(1, "http://localhost:10001");
            tmp2.Add(3, "http://localhost:10003");
            this.ServerList = tmp2;
            this.DataCenter = tmp;
        }

        public int getServerId()
        {
            int id=0;
            foreach(int key in ServerList.Keys)
            {
                if (ServerList[key] == defaultServer)
                {
                    id = key;
                }
            }
            return id;
        }


        public void openChannel(string URL)
        {   
            this.channel = GrpcChannel.ForAddress(URL);
            this.client = new ServerService.ServerServiceClient(this.channel);
            
        }

        public void inputFile(string input_file)

        {
            String[] line = new String[lines.Length];
            Thread[] array = new Thread[lines.Length];
            int count = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                line = lines[i].Split(' ');

                if (line[0]=="begin-repeat")
                {
                    count=beginRepeat(i, int.Parse(line[1]));
                    i = i + count;
                    
                }
                else { switchCase(line); }
                
                 /*
                array[i] = new Thread(() => switchCase(line, i));
                array[i].Start();
                */
            }

        }

        public void  switchCase(string[] line)
        {
             Console.WriteLine(line[0]);
            switch (line[0])
                {
                    case "read":
                        Read(int.Parse(line[1]), int.Parse(line[2]), int.Parse(line[3]));
                        break;
                    case "write":
                        Write(int.Parse(line[1]), int.Parse(line[2]), line[3]);
                        break;
                    case "listServer":
                        ListServer(int.Parse(line[1]));
                        break;
                    case "listGlobal":
                        ListGlobal();
                        break;
                    case "wait":
                        ListServer(int.Parse(line[1]));
                        break;
                    

                
            }
        }
        public ReadResponse Read(int partitionId, int objectId, int server_id)
        {
            
            UniqueKey uniqueKey = new UniqueKey();
            uniqueKey.PartitionId = partitionId;
            uniqueKey.ObjectId = objectId;
            

            ReadResponse response = client.Read(new ReadRequest
            {
                UniqueKey = uniqueKey,
                ServerId = server_id
            });
            
            if (response.Value.Equals("N/A") && server_id != -1)
                {
                Console.WriteLine("Current Server doesn't have the object.Changing Server ...");
                int port = server_id + 10000;
                var change_server = defaultServer.Replace(serverPort, port.ToString());
               openChannel(change_server);
                Console.WriteLine(change_server);
                response = client.Read(new ReadRequest
                {
                    UniqueKey = uniqueKey,
                    ServerId = server_id
                });
                Console.WriteLine("Response from the new server:");
                Console.WriteLine(response);
            }

            Console.WriteLine(response);

            return response;
        }

        public WriteResponse Write(int partitionId, int objectId, string value)
        {
            UniqueKey uniqueKey = new UniqueKey();
            uniqueKey.PartitionId = partitionId;
            uniqueKey.ObjectId = objectId;
            Object o = new Object();
            o.UniqueKey = uniqueKey;
            o.Value = value;


            WriteResponse response = client.Write(new WriteRequest
            {
                Object = o

            });
            Console.WriteLine(response.Ok);
            return response;

        }
        public void ListServer(int server_id)
        {

            ListServerResponse response = client.ListServer(new ListServerRequest
            {
                ServerId = server_id
            });
            foreach (ListServerObj server in response.ListServerObj)
            {
                string output = $" partitionId {server.Object.UniqueKey.PartitionId} " +
                                $"objectId {server.Object.UniqueKey.ObjectId} " +
                                $"value {server.Object.Value}";
                if (server.IsMaster)
                {
                    output += $" Master replica for this object";
                }


                Console.WriteLine(output);
            }
        }
        public void ListGlobal()
        {

            ListGlobalResponse response = client.ListGlobal(new ListGlobalRequest { });
            foreach (UniqueKey server in response.UniqueKeyList)
            {

                string output = $" partitionId {server.PartitionId} " +
                                $"objectId {server.ObjectId} ";

                Console.WriteLine(output);
            }
        }
        public void wait(int x)
        {

            WaitResponse response = client.Wait(new WaitRequest
            {
                X = x
            });
            Thread.Sleep(x);

        }

        public int beginRepeat(int i, int x)
        {
            Console.WriteLine(lines[i].Split(' ')[0]+ " " + lines[i].Split(' ')[1]);
            int count = 0;
            
            //falta verificação duplo begin repeat
            while (!lines[i].Split(' ')[0].Equals("end-repeat") && lines[i].Split(' ')[0].Equals("begin-repeat"))
            {   
                
                for (int l = 0; l < x; l++)
                {
                    switchCase(lines[i+1].Split(' '));
                }
                i = i + 1;
                count = count + 1;
            }
            return count;
        }
        /*
        public List<string> Register(string nick, string port)
        {
            this.nick = nick;
           
            ChatClientRegisterReply reply = client.Register(new ChatClientRegisterRequest
            {
                Nick = nick,
                Url = "http://localhost:" + port
            });

            List<string> result = new List<string>();
            foreach (User u in reply.Users)
            {
                result.Add(u.Nick);
            }
            return result;
        }*/

        static class Program
        {
            /// <summary>
            ///  The main entry point for the application.
            /// </summary>
            [STAThread]
            static void Main(string[] args)
            {
                Client client = new Client("input_file");
                //client.lines = System.IO.File.ReadAllLines("input_file.txt");
                client.inputFile("input_file.txt");
                //client.Register("nknsd", "1000");
                while (true) ;
            }
        }
    }


    /*
    public ChatClientRegisterReply Register(string username, string password)
    {
        CurrentUser.Name = username;

        var reply = Client.Register(
        new ChatClientRegisterRequest { User = CurrentUser, Password = password });

        if (reply.Ok) CurrentUser = reply.User;

        return reply;
    }
}*/
    /*
    public class ChatClientService : ClientService.ClientServiceBase
    {
        ClientService clientLogic;

        public ClientService(IClientService clientLogic)
        {
            this.clientLogic = clientLogic;
        }

        //public override Task<RecvMsgReply> RecvMsg(
        //    RecvMsgRequest request, ServerCallContext context)
        //{
        //   return Task.FromResult(UpdateGUIwithMsg(request));
        //}
        /*
        public RecvMsgReply UpdateGUIwithMsg(RecvMsgRequest request)
        {
            if (clientLogic.AddMsgtoGUI(request.Msg))
            {
                return new RecvMsgReply
                {
                    Ok = true
                };
            }
            else
            {
                return new RecvMsgReply
                {
                    Ok = false
                };

            }
        }
    }*/
}