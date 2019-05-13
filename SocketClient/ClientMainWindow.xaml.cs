using Model;
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
    public partial class ClientMainWindow : Window
    {

        private int BufferSize
        {
            get
            {
                return 8192;
            }
        }

        ////定义Socket对象
        //Socket clientSocket;
        TcpClient clientSocket { get; set; }

        //创建接收消息的线程
        Thread threadReceive;
        //接收服务端发送的数据
        string str;

        ClientMainWindow_ViewModel ViewModel { get; set; }

        public ClientMainWindow()
        {
            InitializeComponent();
            initEvent();
            this.ViewModel = new ClientMainWindow_ViewModel();
            this.DataContext = this.ViewModel;
        }

        private void initEvent()
        {
            this.btnStart.Click += BtnStart_Click;
            this.btnStop.Click += BtnStop_Click;
            this.btnSend.Click += BtnSend_Click;
        }


        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            this.btnStart.IsEnabled = false;

            try
            {
                IPAddress ip = IPAddress.Parse(this.txtIP.Text.Trim());
                int port = Convert.ToInt32(this.txtPort.Text.Trim());
                // clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //连接服务端
                // clientSocket.Connect(ip, port);
                clientSocket = new TcpClient();
                clientSocket.Connect(ip, port); // 开始侦听

                string msg = "Client : Server Connected! Local:{0} --> Server:{1}".FormatWith
                (
                    clientSocket.Client.LocalEndPoint,
                    clientSocket.Client.RemoteEndPoint
                );
                System.Diagnostics.Debug.WriteLine(msg);


                //开启线程不停的接收服务端发送的数据
                threadReceive = new Thread(new ThreadStart(Receive));
                // threadReceive.IsBackground = true;
                threadReceive.Start();

                this.btnStop.IsEnabled = true;
                this.btnSend.IsEnabled = this.btnStop.IsEnabled;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetFullInfo());
                this.btnStart.IsEnabled = true;
            }
        }

        //接收服务端消息的线程方法
        private void Receive()
        {
            try
            {
                while (true) // Stop 后 停止
                {
                    int totalBytesRead = 0; // 读取总长度

                    int startCharIndex = -1;
                    int endCharIndex = -1;

                    byte[] buffOfNetworkStream = new byte[BufferSize];
                    int bytesRead = 0; // 当前读取总长度

                    System.IO.MemoryStream msContent = new System.IO.MemoryStream();

                    try
                    {
                        NetworkStream networkStream = clientSocket.GetStream();

                        bytesRead = networkStream.Read(buffOfNetworkStream, 0, BufferSize);
                        totalBytesRead = totalBytesRead + bytesRead;

                        // 定位 StartChar                        
                        for (int i = 0; i < buffOfNetworkStream.Length; i++)
                        {
                            if ((char)0x02 == Convert.ToChar(buffOfNetworkStream[i]))
                            {
                                startCharIndex = i;
                                break;
                            }
                        }

                        if (startCharIndex < 0)
                        {
                            throw new Exception("缺少(Char)Start");
                        }

                        // 获取内容长度 ( int类型, 共 4 个字节 )
                        int contentLength = BitConverter.ToInt32(buffOfNetworkStream, startCharIndex + 1); // 内容长度
                        msContent.Write // 写入内容
                        (
                            buffOfNetworkStream,
                            startCharIndex + 1 + 4, // (Char)Start 起始位置 + 1( (char)Start 1 个字节 ) + 4( 内容长度 4 个字节 )
                            bytesRead - (startCharIndex + 1 + 4)
                        );

                        while (totalBytesRead < 1 + 4 + contentLength + 1)
                        {
                            bytesRead = networkStream.Read(buffOfNetworkStream, 0, BufferSize);
                            totalBytesRead = totalBytesRead + bytesRead;
                            msContent.Write(buffOfNetworkStream, 0, bytesRead);
                        }
                    }
                    catch (System.IO.IOException ioEx)
                    {
                        string msg = "报错:{0}".FormatWith(ioEx.Message);
                        System.Diagnostics.Debug.WriteLine(msg);
                        break;
                    }

                    byte[] contentByteArr = msContent.GetBuffer();
                    str = Encoding.UTF8.GetString(contentByteArr, 0, contentByteArr.Length);

                    // 定位 EndChar
                    endCharIndex = str.IndexOf((char)0x03);
                    if (endCharIndex < 0)
                    {
                        throw new Exception("缺少(Char)End");
                    }

                    str = str.Substring(0, endCharIndex);

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        var toAdd = new MyMessage()
                        {
                            ReceiveTime = DateTime.Now,
                            Content = str,
                            Length = str.Length
                        };

                        this.ViewModel.ReceiveList.Add(toAdd);

                        dg1.ScrollIntoView(toAdd);
                    }));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetFullInfo());
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            this.btnStop.IsEnabled = false;
            try
            {
                clientSocket.Client.Close();

                this.btnStart.IsEnabled = true;
                this.btnSend.IsEnabled = !this.btnStart.IsEnabled;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetFullInfo());
                this.btnStop.IsEnabled = true;
            }
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NetworkStream streamToServer = clientSocket.GetStream();

                string strMsg = this.txtToSend.Text.Trim();
                byte[] strBuffer = Encoding.UTF8.GetBytes(strMsg);

                Model.SocketModel socketModel = new Model.SocketModel();
                socketModel.Content = strBuffer;

                byte[] buffer = socketModel.ToByteArray();
                streamToServer.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetFullInfo());
            }
        }
    }

    public class ClientMainWindow_ViewModel : Client.ViewModel.BaseViewModel
    {
        public ClientMainWindow_ViewModel()
        {
            this.ReceiveList = new Util.UIComponent.BaseCollection<MyMessage>();
        }

        private Util.UIComponent.BaseCollection<MyMessage> _ReceiveList;

        public Util.UIComponent.BaseCollection<MyMessage> ReceiveList
        {
            get { return _ReceiveList; }
            set
            {
                if (_ReceiveList != null)
                {
                    _ReceiveList.CollectionChanged -= _ReceiveList_CollectionChanged;
                }

                _ReceiveList = value;
                this.OnPropertyChanged("ReceiveList");

                if (_ReceiveList != null)
                {
                    _ReceiveList.CollectionChanged += _ReceiveList_CollectionChanged;
                }
            }
        }

        private void _ReceiveList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.OnPropertyChanged("ReceiveList");
        }
    }
}
