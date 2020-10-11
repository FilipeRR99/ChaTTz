using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Grpc.Net.Client;

namespace ChatClient
{
    public partial class StartingPage : Form
    {
        private string ServerUrl;
        private GrpcChannel channel;
        private ChatServerService.ChatServerServiceClient client;

        public StartingPage(string server_url)
        {
            InitializeComponent();
            ServerUrl = server_url;

            AppContext.SetSwitch(
            "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            channel = GrpcChannel.ForAddress(ServerUrl);
            client = new ChatServerService.ChatServerServiceClient(channel);
        }

        private void OpenChatPage()
        {
            Form frm = new ChatPage();
            frm.Location = this.Location;
            frm.StartPosition = FormStartPosition.Manual;
            frm.FormClosing += delegate { this.Show(); };
            frm.Show();
            this.Hide();
        }
    }
}
