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
        //定义Socket对象
        TcpClient tcpClient { get; set; }

        //创建接收消息的线程
        Task taskReceive;

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
                tcpClient = new TcpClient();
                tcpClient.Connect(ip, port); // 开始侦听

                string msg = "Client : Server Connected! Local:{0} --> Server:{1}".FormatWith
                (
                    tcpClient.Client.LocalEndPoint,
                    tcpClient.Client.RemoteEndPoint
                );
                System.Diagnostics.Debug.WriteLine(msg);


                //开启线程不停的接收服务端发送的数据
                taskReceive = new Task(() => Receive());
                taskReceive.ContinueWith((task) =>
                {
                    System.Diagnostics.Debug.WriteLine("client taskReceive finish");
                });

                // threadReceive.IsBackground = true;
                taskReceive.Start();

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
            while (true) // TODO 处理 Stop 后
            {
                try
                {
                    string str = tcpClient.Receive(); // 自定义扩展方法

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
                catch (System.IO.IOException ioException)
                {
                    if (tcpClient.Connected == false)
                    {
                        break;
                    }

                    string msg = "{0}".FormatWith(ioException.GetFullInfo());
                    System.Diagnostics.Debug.WriteLine(msg);

                    throw ioException;                    
                }
                catch (Exception ex)
                {
                    string msg = "{0}".FormatWith(ex.GetFullInfo());
                    System.Diagnostics.Debug.WriteLine(msg);
                }
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            this.btnStop.IsEnabled = false;
            try
            {
                tcpClient.Client.Close();

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
                tcpClient.Send(this.txtToSend.Text.TrimAdv()); // 自定义扩展方法
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
