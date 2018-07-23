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
    public partial class Form1 : Form
    {
        Socket socket;
        IPAddress thisAddress;
        List<Socket> connectedClients;
        public Form1()
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

            // 주소가 없다면..
            if (thisAddress == null)
                // 로컬호스트 주소를 사용한다.
                thisAddress = IPAddress.Loopback;

            txtIP.Text = thisAddress.ToString();
        }

        // btnSend
        private void button1_Click(object sender, EventArgs e)
        {
            // 서버가 대기중인지 확인한다.
            if (!socket.IsBound)
            {
                //MsgBoxHelper.Warn("서버가 실행되고 있지 않습니다!");
                txtStatus.AppendText("서버를 실행해주세요.\n");
                return;
            }

            // 보낼 텍스트
            string tts = txtContent.Text.Trim();
            if (string.IsNullOrEmpty(tts))
            {
                //MsgBoxHelper.Warn("텍스트가 입력되지 않았습니다!");
                txtStatus.AppendText("텍스트를 입력해주세요.\n");
                txtContent.Focus();
                return;
            }

            // 문자열을 utf8 형식의 바이트로 변환한다.
            byte[] bDts = Encoding.UTF8.GetBytes(thisAddress.ToString() + '\x01' + tts);

            // 연결된 모든 클라이언트에게 전송한다.
            for (int i = connectedClients.Count - 1; i >= 0; i--)
            {
                Socket socket = connectedClients[i];
                try { socket.Send(bDts); }
                catch
                {
                    // 오류 발생하면 전송 취소하고 리스트에서 삭제한다.
                    try { socket.Dispose(); } catch { }
                    connectedClients.RemoveAt(i);
                }
            }

            // 전송 완료 후 텍스트박스에 추가하고, 원래의 내용은 지운다.
            txtStatus.AppendText(string.Format("[보냄]{0}: {1}\n", thisAddress.ToString(), tts));
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

            // Open the Socket
            IPEndPoint ep = new IPEndPoint(thisAddress, port); // Param : IP, Port
            socket.Bind(ep); // EndPoint : IP + Port
            socket.Listen(10); // 연결 요청 보류 큐 제한 

            // accept request from user by asynchronous
            socket.BeginAccept(AcceptCallback, null);
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
            // BeginReceive에서 추가적으로 넘어온 데이터를 AsyncObject 형식으로 변환한다.
            AsyncObject obj = (AsyncObject)ar.AsyncState;

            // 데이터 수신을 끝낸다.
            int received = obj.WorkingSocket.EndReceive(ar);

            // 받은 데이터가 없으면(연결끊어짐) 끝낸다.
            if (received <= 0)
            {
                obj.WorkingSocket.Close();
                return;
            }

            // 텍스트로 변환한다.
            string text = Encoding.UTF8.GetString(obj.Buffer);

            // 0x01 기준으로 짜른다.
            // tokens[0] - 보낸 사람 IP
            // tokens[1] - 보낸 메세지
            string[] tokens = text.Split('\x01');
            string ip = tokens[0];
            string msg = tokens[1];

            // 텍스트박스에 추가해준다.
            // 비동기식으로 작업하기 때문에 폼의 UI 스레드에서 작업을 해줘야 한다.
            // 따라서 대리자를 통해 처리한다.
            txtStatus.AppendText(string.Format("[받음]{0}: {1}\n", ip, msg));

            // for을 통해 "역순"으로 클라이언트에게 데이터를 보낸다.
            for (int i = connectedClients.Count - 1; i >= 0; i--)
            {
                Socket socket = connectedClients[i];
                if (socket != obj.WorkingSocket)
                {
                    try { socket.Send(obj.Buffer); }
                    catch
                    {
                        // 오류 발생하면 전송 취소하고 리스트에서 삭제한다.
                        try { socket.Dispose(); } catch { }
                        connectedClients.RemoveAt(i);
                    }
                }
            }

            // 데이터를 받은 후엔 다시 버퍼를 비워주고 같은 방법으로 수신을 대기한다.
            obj.ClearBuffer();

            // 수신 대기
            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj);
        }
    }
}
