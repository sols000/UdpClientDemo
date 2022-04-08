using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UdpServerSide
{

    public struct UdpState
    {
        public UdpClient u;
        public IPEndPoint e;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        UdpClient udpClient;

        private void OnMainInitialized(object sender, EventArgs e)
        {

        }

        int ServerListenPort(int port)
        {

            IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
            try
            {
                udpClient = new UdpClient(ep);
            }
            catch (Exception)
            {
                return -1;
            }
            UdpState state = new UdpState();
            state.e = ep;
            state.u = udpClient;

            Trace.TraceInformation($"listening for messages At Port:{port}");
            udpClient.BeginReceive(new AsyncCallback(ReceiveCallBack), state);
            return 0;
        }

        IPEndPoint remoteEndPoint = null;

        private void ReceiveCallBack(IAsyncResult ar)
        {
            UdpClient u = ((UdpState)ar.AsyncState).u;
            IPEndPoint e = ((UdpState)ar.AsyncState).e;
            byte[] receiveBytes = null;
            try
            {
                receiveBytes = u.EndReceive(ar, ref e);
            }
            catch (Exception erExp)
            {
                Trace.TraceWarning($"Remote Client may be closed:{erExp.Message}");
                //Incase of client reconnect
                u.BeginReceive(new AsyncCallback(ReceiveCallBack), ar.AsyncState);
                return;
            }

            string receiveString = Encoding.UTF8.GetString(receiveBytes);

            Trace.TraceInformation($"Received: {receiveString}");

            ProcessMessage(e, receiveString);

            u.BeginReceive(new AsyncCallback(ReceiveCallBack), ar.AsyncState);
            return;
        }

        private void ProcessMessage(IPEndPoint e, string strMsg)
        {
            Dispatcher.Invoke(() =>
            {
                MainContent.Items.Add(strMsg);
                Decorator decorator = (Decorator)VisualTreeHelper.GetChild(MainContent, 0);
                ScrollViewer scrollViewer = (ScrollViewer)decorator.Child;
                scrollViewer.ScrollToEnd();

                if (strMsg.Equals("Connect"))
                {
                    if (!e.Equals(remoteEndPoint))
                    {
                        remoteEndPoint = e;
                        ClientInfoLable.Text = remoteEndPoint.ToString();
                    }
                    sendUdpMessage("ConnectOK");
                }

            });
        }

        private void OnListenBtnClicked(object sender, RoutedEventArgs e)
        {
            int lPort = int.Parse(listenPort.Text);
            int res = ServerListenPort(lPort);
            if (res == 0)
            {
                ClientInfoLable.Text = $"Listening on port:{lPort}";
            }
            else if (res == -1)
            {
                ClientInfoLable.Text = $"Port {lPort} Not Valid";
            }
            Trace.TraceInformation("Listen Res: {0}", res);
        }

        int sendUdpMessage(string strMsg)
        {
            if (strMsg.Length == 0)
            {
                Trace.TraceWarning("No Valid data");
                return -2;
            }
            if (udpClient == null || remoteEndPoint == null)
            {
                Trace.TraceWarning("Remote End Point Not Connected");
                return -1;
            }
            byte[] datas = Encoding.UTF8.GetBytes(strMsg);
            var sendTask = udpClient.SendAsync(datas, datas.Length, remoteEndPoint);
            sendTask.ContinueWith(t =>
            {
                Trace.TraceInformation($"SendData Size: {t.Result}");
            });
            return 0;
        }

        void DoSendUDPMessage()
        {
            int res = sendUdpMessage(MsgToSend.Text);
            if (res == 0)
            {
                MsgToSend.Clear();
            }
            MsgToSend.Focus();
        }

        private void OnSendMsgClicked(object sender, RoutedEventArgs e)
        {
            DoSendUDPMessage();
        }

        private void OnClearContentClicked(object sender, RoutedEventArgs e)
        {
            MainContent.Items.Clear();
        }

        private void OnKeyboardEvent(object sender, KeyEventArgs e)
        {
            Trace.TraceInformation($"Key Clicked:{e.Key}");
            if (e.Key == Key.Return)
            {
                DoSendUDPMessage();
            }
        }
    }
}
