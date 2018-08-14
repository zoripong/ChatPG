using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatN1S
{
    public partial class ChatServer : Form
    {
        Socket socket;
        IPAddress thisAddress;
        List<Socket> connectedClients;

        public ChatServer()
        {
            InitializeComponent();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            connectedClients = new List<Socket>();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            IPHostEntry entry = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress addr in entry.AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    thisAddress = addr;
                    break;
                }
            }

            
            if (thisAddress == null)
                thisAddress = IPAddress.Loopback; // localhost

            txtIP.Text = thisAddress.ToString();
        }

        // btnSend
        private void button1_Click(object sender, EventArgs e)
        {
           
            if (!socket.IsBound)
            {
                txtStatus.AppendText("서버를 실행해주세요.\n");
                return;
            }

            string tts = txtContent.Text.Trim();
            if (string.IsNullOrEmpty(tts))
            {
                txtStatus.AppendText("텍스트를 입력해주세요.\n");
                txtContent.Focus();
                return;
            }

            byte[] bDts = Encoding.UTF8.GetBytes(thisAddress.ToString() + '\x01' + tts);

            // 클라이언트에 전송
            for (int i = connectedClients.Count - 1; i >= 0; i--)
            {
                Socket socket = connectedClients[i];
                try { socket.Send(bDts); }
                catch
                {
                    try { socket.Dispose(); } catch { }
                    connectedClients.RemoveAt(i);
                }
            }

            txtStatus.AppendText(string.Format("[보냄]{0}: {1}", thisAddress.ToString(), tts));
            txtStatus.AppendText("\n");
            txtContent.Clear();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            int port;
            if (!int.TryParse(txtPort.Text, out port))
            {
                txtStatus.AppendText("포트번호가 잘못되었습니다. 확인해주세요.\n");
                txtPort.Focus();
                txtPort.SelectAll();
                return;
            }

            if (port < 1024 || port > 65535)
            {
                txtStatus.AppendText("포트번호가 범위를 벗어났습니다.\n");
                txtPort.Focus();
                txtPort.SelectAll();
                return;
            }


            // Open the Socket
            IPEndPoint ep = new IPEndPoint(thisAddress, port); // Param : IP, Port
            socket.Bind(ep); // EndPoint : IP + Port
            socket.Listen(10); // 연결 요청 보류 큐 제한 

            // accept request from user by asynchronous
            socket.BeginAccept(AcceptCallback, null);

            txtStatus.AppendText("Server is Running on " + txtIP.Text + "\n");
            txtStatus.AppendText("Port number is " + txtPort.Text + "\n");
            txtPort.ReadOnly = true;

            btnOpen.Enabled = false;
            
            
        }


        

        void AcceptCallback(IAsyncResult ar)
        {
            
            Socket client = socket.EndAccept(ar); // accept request
            socket.BeginAccept(AcceptCallback, null); // waiting the request 

            AsyncObject obj = new AsyncObject(4096);
            obj.WorkingSocket = client;

            connectedClients.Add(client); // add the client to list

            txtStatus.AppendText(string.Format("클라이언트가 추가 되었습니다. [@{0}]\n", client.RemoteEndPoint));
            
            client.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj); // waiting data from user
        }

        void DataReceived(IAsyncResult ar)
        {
            //convert
            AsyncObject obj = (AsyncObject)ar.AsyncState;

            // end the receiving
            int received = obj.WorkingSocket.EndReceive(ar);

            if (received <= 0)
            {
                obj.WorkingSocket.Close();
                return;
            }

            string text = Encoding.UTF8.GetString(obj.Buffer);

            string[] tokens = text.Split('\x01');
            string ip = tokens[0];
            string name = tokens[1];
            string msg = tokens[2];

            txtStatus.AppendText(string.Format("[받음]{0}({1}): {2}", name, ip, msg));
            txtStatus.AppendText("\n");

            
            for (int i = connectedClients.Count - 1; i >= 0; i--)
            {
                Socket socket = connectedClients[i];
                if (socket != obj.WorkingSocket)
                {
                    try { socket.Send(obj.Buffer); }
                    catch
                    {
                        try { socket.Dispose(); } catch { }
                        connectedClients.RemoveAt(i);
                    }
                }
            }

            
            obj.ClearBuffer();

            // 수신 대기
            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj);
        }
    }
}
