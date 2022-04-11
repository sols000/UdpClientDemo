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

namespace UdpClientDemo
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
        private UdpClient mUdpClient;

        public MainWindow()
        {
            InitializeComponent();
        }


        private void OnMainInitialized(object sender, EventArgs e)
        {

        }


        private int ConnectUdp(string hostName, int port)
        {

            IPHostEntry host;
            try
            {
                host = Dns.GetHostEntry(hostName);
            }
            catch (Exception ResE)
            {
                Trace.TraceError($"Con not GetHostEntry of {hostName} ,Error:{ ResE.Message}");
                return -1;
            }
            if (host.AddressList.Length < 1)
            {
                Trace.TraceWarning("HostEntry Found {0} Addr, ", host.AddressList.Length);
                return -3;
            }
            else
            {
                foreach(var ipAddr in host.AddressList)
                {
                    Trace.TraceInformation("Find IpAddr:"+ipAddr.ToString());
                }
            }

            try
            {
                mUdpClient.Connect(hostName, port);
            }
            catch (Exception e)
            {
                Trace.TraceError("Connect Error:{0}", e.Message);
                return -2;
            }

            return 0;
        }

        private int ConnectUdpWithIp(IPAddress addr, int port)
        {
            try
            {
                mUdpClient.Connect(addr, port);
            }
            catch (Exception e)
            {
                Trace.TraceError($"Connect With Ip Error:{e.Message}");
                return -4;
            }
            return 0;
        }

        int BeginReceive()
        {
            sendUdpMessage("Connect");

            UdpState state = new UdpState();
            state.u = mUdpClient;
            state.e = null;
            try
            {
                mUdpClient.BeginReceive(new AsyncCallback(ReceiveCallBack), state);
            }
            catch (Exception e)
            {
                Trace.TraceError("BeginReceive Error:{0}", e.Message);
                return -4;
            }
            return 0;
        }


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
                return;
            }

            string receiveString = Encoding.UTF8.GetString(receiveBytes);
            if (receiveString.Length > 0)
            {
                ProcessMessage(e, receiveString);
            }

            u.BeginReceive(new AsyncCallback(ReceiveCallBack), ar.AsyncState);
            return;
        }

        private void ProcessMessage(IPEndPoint e, string strMsg)
        {
            Dispatcher.Invoke(() =>
            {

                if (strMsg.Equals("ConnectOK"))
                {
                    NetOperation.SelectedIndex = 1;
                    RemoteAddrLable.Content = e.ToString();
                    LocalAddrLable.Content = mUdpClient.Client.LocalEndPoint.ToString();
                    MainContent.Items.Clear();
                }
                MainContent.Items.Add(strMsg);
                Decorator decorator = (Decorator)VisualTreeHelper.GetChild(MainContent, 0);
                ScrollViewer scrollViewer = (ScrollViewer)decorator.Child;
                scrollViewer.ScrollToEnd();

            });
        }

        void ConnectUdpNetWork()
        {
            string strHostName = RemoteHostNameLable.Text;
            string strServerPort = ServerPortLable.Text;
            int portNumber = int.Parse(strServerPort);
            //Test if it's a ip addr like : 192.168.15.17
            IPAddress tempIpAddr = null;

            //new a udpclient
            try
            {
                mUdpClient = new UdpClient();
                tempIpAddr = IPAddress.Parse(strHostName);
            }
            catch (Exception excpt)
            {
                Trace.TraceError("SocketException:{0}", excpt.Message);
            }

            int res;
            if (tempIpAddr != null)
            {
                //for ip addr
                res = ConnectUdpWithIp(tempIpAddr, portNumber);
            }
            else
            {
                //for host name
                res = ConnectUdp(strHostName, portNumber);
            }
            if (res < 0)
            {
                return;
            }
            Trace.TraceInformation("Connect Udp Res:{0}", res);
            BeginReceive();

        }

        private void OnSendConnectClicked(object sender, RoutedEventArgs e)
        {
            ConnectUdpNetWork();
        }

        int sendUdpMessage(string strMsg)
        {
            if (mUdpClient == null)
            {
                Trace.TraceWarning("mUdpClient is null");
                return -3;
            }
            if (strMsg.Length == 0)
            {
                Trace.TraceWarning("No Valid data");
                return -2;
            }
            if (mUdpClient == null)
            {
                Trace.TraceWarning("Remote End Point Not Connected");
                return -1;
            }
            byte[] datas = Encoding.UTF8.GetBytes(strMsg);
            var sendTask = mUdpClient.SendAsync(datas, datas.Length);
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

        private void OnKeyboardEvent(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                DoSendUDPMessage();
            }
        }


        private void OnDisConnectBtn(object sender, RoutedEventArgs e)
        {
            //notify the remote end a disconnect message
            byte[] datas = Encoding.UTF8.GetBytes("DisConnect");
            mUdpClient.Send(datas, datas.Length);
            mUdpClient.Close();
            mUdpClient = null;
            NetOperation.SelectedIndex = 0;
        }
    }
}
