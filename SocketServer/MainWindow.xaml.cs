using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

namespace SocketServer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //定义Socket对象
        Socket serverSocket;
        //定义监听线程
        Thread listenThread;
        //定义接收客户端数据线程
        Thread threadReceive;
        //定义双方通信
        Socket socket;
        string str;


        public MainWindow()
        {
            InitializeComponent();
            initEvent();
        }

        private void initEvent()
        {
            this.btnStart.Click += BtnStart_Click;
            this.btnStop.Click += BtnStop_Click;
            this.btnSend.Click += BtnSend_Click;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (serverSocket != null && serverSocket.Connected == true)
            {
                MessageBox.Show("ServerSocket is Connected");
                return;
            }

            IPAddress ip = IPAddress.Parse(this.txtIP.Text.Trim());
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                //绑定ip和端口
                serverSocket.Bind(new IPEndPoint(ip, Convert.ToInt32(this.txtPort.Text.Trim())));
                //设置最多10个排队连接请求
                serverSocket.Listen(10);
                //开启线程循环监听
                listenThread = new Thread(ListenClientConnect);
                listenThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetFullInfo());
            }
        }

        //监听
        private void ListenClientConnect()
        {
            while (true)
            {
                //监听到客户端的连接，获取双方通信socket
                socket = serverSocket.Accept();
                //创建线程循环接收客户端发送的数据
                threadReceive = new Thread(Receive);
                //传入双方通信socket
                threadReceive.Start(socket);
            }
        }

        //接收客户端数据
        private void Receive(object socket)
        {
            try
            {
                Socket myClientSocket = (Socket)socket;
                while (true)
                {
                    byte[] buff = new byte[20000];
                    int r = myClientSocket.Receive(buff);
                    str = Encoding.Default.GetString(buff, 0, r);
                    this.Dispatcher.Invoke(new Action(() => { this.txtReceive.Text += "\r\n{0}".FormatWith(str); }));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetFullInfo());
            }
        }


        //关闭
        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //socket关闭
                serverSocket.Close();
                //线程关闭
                listenThread.Abort();
                threadReceive.Abort();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetFullInfo());
            }
        }



        //发送
        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string strMsg = this.txtToSend.Text.Trim();
                byte[] buffer = Encoding.Default.GetBytes(strMsg);
                socket.Send(buffer);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetFullInfo());
            }
        }
    }
}
