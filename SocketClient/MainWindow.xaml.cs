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

namespace SocketClient
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //定义Socket对象
        Socket clientSocket;
        //创建接收消息的线程
        Thread threadReceive;
        //接收服务端发送的数据
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
            IPAddress ip = IPAddress.Parse(this.txtIP.Text.Trim());
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                //连接服务端
                clientSocket.Connect(ip, Convert.ToInt32(this.txtPort.Text.Trim()));
                //开启线程不停的接收服务端发送的数据
                threadReceive = new Thread(new ThreadStart(Receive));
                threadReceive.IsBackground = true;
                threadReceive.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetFullInfo());
            }
        }

        //接收服务端消息的线程方法
        private void Receive()
        {
            try
            {
                while (true)
                {
                    byte[] buff = new byte[20000];
                    int r = clientSocket.Receive(buff);
                    str = Encoding.Default.GetString(buff, 0, r);
                    this.Dispatcher.Invoke(new Action(() => { this.txtReceive.Text += "\r\n{0}".FormatWith(str); }));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetFullInfo());
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            //clientSocket关闭
            clientSocket.Close();
            //threadReceive关闭
            threadReceive.Abort();
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string strMsg = this.txtToSend.Text.Trim();
                byte[] buffer = Encoding.Default.GetBytes(strMsg);
                clientSocket.Send(buffer);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetFullInfo());
            }
        }
    }
}
