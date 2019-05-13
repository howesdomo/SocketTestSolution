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

namespace SocketServer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ServerMainWindow : Window
    {
        /// <summary>
        /// 大多数文件系统都配置为使用4096或8192的块大小。
        /// 理论上，如果配置缓冲区大小使得读取比磁盘块多几个字节，
        /// 则使用文件系统的操作可能效率极低（即，如果您将缓冲区配置为一次读取4100个字节，
        /// 每次读取将需要文件系统进行2次块读取。如果块已经在缓存中，
        /// 那么你最终会支付RAM的价格 - > L3 / L2缓存延迟。
        /// 如果你运气不好并且块还没有缓存，那么你也需要支付磁盘 - > RAM延迟的价格。
        /// </summary>
        private int BufferSize
        {
            get
            {
                return 8192;
            }
        }

        //定义Socket对象
        // Socket serverSocket;
        TcpListener serverSocket;

        //定义监听线程
        // Thread listenThread;
        Task taskListen;

        //定义接收客户端数据线程
        // Thread threadReceive;
        Task taskReceive;


        //定义双方通信
        // Socket socket;
        TcpClient remoteClient;
        string str;


        ServerMainWindow_ViewModel ViewModel { get; set; }

        public ServerMainWindow()
        {
            InitializeComponent();
            initEvent();
            this.ViewModel = new ServerMainWindow_ViewModel();
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

                serverSocket = new TcpListener(ip, port);
                serverSocket.Start();

                string msg = "Server : Start Listening";
                System.Diagnostics.Debug.WriteLine(msg);

                taskListen = new Task(ListenClientConnect);
                taskListen.Start();

                this.btnStop.IsEnabled = true;
                this.btnSend.IsEnabled = this.btnStop.IsEnabled;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetFullInfo());
                this.btnStart.IsEnabled = true;
            }
        }

        //监听
        private void ListenClientConnect()
        {
            while (true)
            {
                //监听到客户端的连接，获取双方通信socket
                remoteClient = serverSocket.AcceptTcpClient();
                string msg = "Server : Client Connected! Local:{0} <-- Client:{1}".FormatWith
                (
                    remoteClient.Client.LocalEndPoint,
                    remoteClient.Client.RemoteEndPoint
                );
                System.Diagnostics.Debug.WriteLine(msg);

                //创建线程循环接收客户端发送的数据
                taskReceive = new Task(() => Receive(remoteClient));
                taskReceive.ContinueWith((task) =>
                {
                    System.Diagnostics.Debug.WriteLine("taskReceive is end");
                });

                //传入双方通信socket
                taskReceive.Start();
            }
        }

        //接收客户端数据
        private void Receive(TcpClient myClientSocket)
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
                        NetworkStream networkStream = myClientSocket.GetStream();

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


        //关闭
        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            this.btnStop.IsEnabled = false;
            try
            {
                serverSocket.Stop();

                this.btnStart.IsEnabled = true;
                this.btnSend.IsEnabled = !this.btnStart.IsEnabled;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetFullInfo());
                this.btnStop.IsEnabled = true;
            }
        }

        //发送
        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NetworkStream streamToClient = remoteClient.GetStream();

                string strMsg = this.txtToSend.Text.Trim();
                byte[] strBuffer = Encoding.UTF8.GetBytes(strMsg);

                Model.SocketModel socketModel = new Model.SocketModel();
                socketModel.Content = strBuffer;

                byte[] buffer = socketModel.ToByteArray();
                streamToClient.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetFullInfo());
            }
        }
    }

    public class ServerMainWindow_ViewModel : Client.ViewModel.BaseViewModel
    {
        public ServerMainWindow_ViewModel()
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
