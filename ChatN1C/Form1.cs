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

namespace ChatN1C
{
    public partial class Form1 : Form
    {
        Socket socket;

        public Form1()
        {
            InitializeComponent();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            IPHostEntry he = Dns.GetHostEntry(Dns.GetHostName());

            IPAddress defaultHostAddress = null;
            foreach (IPAddress addr in he.AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    defaultHostAddress = addr;
                    break;
                }
            }

            if (defaultHostAddress == null)
                defaultHostAddress = IPAddress.Loopback; // localhost

            txtIP.Text = defaultHostAddress.ToString();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (socket.Connected)
            {
                txtStatus.AppendText("이미 연결이 되어있습니다.\n");
                return;
            }


            
            int port;

            if (!ValidateIPv4(txtIP.Text))
            {
                txtStatus.AppendText("IP가 범위를 벗어났습니다.\n");
                txtIP.Focus();
                txtIP.SelectAll();
                return;
            }


            if (!int.TryParse(txtPort.Text, out port))
            {
                txtStatus.AppendText("포트번호를 확인해주세요.\n");
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


            try {
                socket.Connect(txtIP.Text, port);
            }catch (Exception ex){
                txtStatus.AppendText("연결 실패\n");
                txtStatus.AppendText(string.Format("error : {0}\n", ex.Message));

                return;
            }

            txtStatus.AppendText("서버 연결 성공\n");

            AsyncObject obj = new AsyncObject(4096);
            obj.WorkingSocket = socket;
            socket.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);
        }

        public bool ValidateIPv4(string ipString)
        {
            if (String.IsNullOrWhiteSpace(ipString))
            {
                return false;
            }

            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            byte tempForParsing;

            return splitValues.All(r => byte.TryParse(r, out tempForParsing));
        }

        void DataReceived(IAsyncResult ar)
        {
            AsyncObject obj = (AsyncObject)ar.AsyncState;

            int received = obj.WorkingSocket.EndReceive(ar);

            if (received <= 0)
            {
                obj.WorkingSocket.Close();
                return;
            }

            string text = Encoding.UTF8.GetString(obj.Buffer);

            
            string[] tokens = text.Split('\x01');
            string ip = tokens[0];
            string msg = tokens[1];


            txtStatus.AppendText(string.Format("[받음]Server({0}): {1}", ip, msg));
            txtStatus.AppendText("\n");
            obj.ClearBuffer();


            // 수신 대기
            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj);
        }

      

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (!socket.IsBound)
            {
                txtStatus.AppendText("서버와 연결해주세요.\n");
                return;
            }

            string tts = txtContent.Text.Trim();
            if (string.IsNullOrEmpty(tts))
            {
                txtStatus.AppendText("텍스트가 입력되지 않았습니다!\n");
                txtContent.Focus();
                return;
            }

            IPEndPoint ip = (IPEndPoint)socket.LocalEndPoint;
            string addr = ip.ToString();

            string userName = txtName.Text;

            byte[] bDts = Encoding.UTF8.GetBytes(addr + '\x01' + userName + '\x01' + tts);

            socket.Send(bDts);

            txtStatus.AppendText(string.Format("[보냄]{0}: {1}", addr, tts));
            txtStatus.AppendText("\n");

            txtContent.Clear();
        }

        private void txtStatus_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
