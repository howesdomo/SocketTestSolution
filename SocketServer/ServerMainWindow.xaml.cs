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
        //定义Socket对象
        TcpListener mTcpListener { get; set; }

        //定义监听线程
        Task taskListen { get; set; }

        //定义接收客户端数据线程
        Task taskReceive { get; set; }

        //定义双方通信
        LinkedList<TcpClient> remoteClientLinkedList { get; set; } = new LinkedList<TcpClient>();

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

                mTcpListener = new TcpListener(ip, port);
                mTcpListener.Start();

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

        // 监听
        private void ListenClientConnect()
        {
            while (true)
            {
                // 监听到客户端的连接，获取双方通信socket
                TcpClient remoteClient = mTcpListener.AcceptTcpClient();
                remoteClientLinkedList.AddLast(remoteClient);

                string msg = "Server : Client Connected! Local:{0} <-- Client:{1}".FormatWith
                (
                    remoteClient.Client.LocalEndPoint,
                    remoteClient.Client.RemoteEndPoint
                );
                System.Diagnostics.Debug.WriteLine(msg);

                // 创建线程循环接收客户端发送的数据
                taskReceive = new Task(() => Receive(remoteClient));
                taskReceive.ContinueWith((task) =>
                {
                    System.Diagnostics.Debug.WriteLine("taskReceive is end");
                });

                // 传入双方通信socket
                taskReceive.Start();
            }
        }

        // 接收客户端数据
        private void Receive(TcpClient client)
        {
            while (true)
            {
                string str = client.Receive(); // 自定义扩展方法

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


        // 关闭
        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            this.btnStop.IsEnabled = false;
            try
            {
                mTcpListener.Stop();

                this.btnStart.IsEnabled = true;
                this.btnSend.IsEnabled = !this.btnStart.IsEnabled;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetFullInfo());
                this.btnStop.IsEnabled = true;
            }
        }

        // 发送
        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<TcpClient> toDeleteList = new List<TcpClient>();

                foreach (var remoteClient in this.remoteClientLinkedList)
                {
                    if (remoteClient.Connected == false) // 收集已被断开的连接, 待删除
                    {
                        toDeleteList.Add(remoteClient);
                    }
                    else
                    {
                        remoteClient.Send(this.txtToSend.Text.TrimAdv()); // 自定义扩展方法
                    }
                }

                foreach (var toDel in toDeleteList)
                {
                    // 删除已断开的连接
                    this.remoteClientLinkedList.Remove(toDel);
                }

                toDeleteList.Clear();
                toDeleteList = null;
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
