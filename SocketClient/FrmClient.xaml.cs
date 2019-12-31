using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Util.Web;

namespace SocketClient
{
    /// <summary>
    /// FrmClient.xaml 的交互逻辑
    /// </summary>
    public partial class FrmClient : Window
    {

        FrmClient_ViewModel ViewModel { get; set; }
        public FrmClient()
        {
            InitializeComponent();
            this.Title = $"{this.Title} - V {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
            this.ViewModel = new FrmClient_ViewModel(this);
            this.DataContext = this.ViewModel;
        }
    }

    public class FrmClient_ViewModel : Client.ViewModel.BaseViewModel
    {
        FrmClient frm { get; set; }

        Util.Web.MyTcpClient mMyTcpClient { get; set; }

        public Command CMD_ConnectServer { get; set; }

        public Command CMD_DisconnectServer { get; set; }

        public Command CMD_Send { get; set; }

        public Command CMD_StandardSend { get; set; }


        public FrmClient_ViewModel(FrmClient frm)
        {
            this.frm = frm;

            CMD_ConnectServer = new Command(() => { mMyTcpClient.Start(this.IP, this.Port); });
            CMD_DisconnectServer = new Command(() => { mMyTcpClient.Stop(); });
            CMD_Send = new Command(() =>
            {
                if (this.SendContent.IsNullOrWhiteSpace() == false)
                {
                    mMyTcpClient.Send(this.SendContent);
                }
            });

            CMD_StandardSend = new Command(() =>
            {
                if (this.SendContent.IsNullOrWhiteSpace() == false)
                {
                    mMyTcpClient.StandardSend(this.SendContent);
                }
            });

            mMyTcpClient = new Util.Web.MyTcpClient();
            mMyTcpClient.ReceiveText += new EventHandler<TcpXxxEventArgs>(receiveText_Handle);
            mMyTcpClient.StatusChange += new EventHandler<TcpXxxStatusChangeEventArgs>(tcpServer_StatusChange);
        }

        private string _SendContent;
        public string SendContent
        {
            get
            {
                return _SendContent;
            }
            set
            {
                _SendContent = value;
                this.OnPropertyChanged("SendContent");
            }
        }

        private void receiveText_Handle(object sender, TcpXxxEventArgs args)
        {
            string receiveMsg = args.Msg;
            var toAdd = new Util.Model.ConsoleData($"信息长度:{args.Msg.Length}\r\n{args.Msg}", Util.Model.ConsoleMsgType.DEFAULT);

            frm.Dispatcher.Invoke(() =>
            {
                frm.ucConsole_ReceiveInfos.Add(toAdd);
            });
        }

        private void tcpServer_StatusChange(object sender, TcpXxxStatusChangeEventArgs args)
        {
            ServerInfo = string.Format("服务器{0}中, 正在连接共 {1} 个客户端", args.IsConnect ? "开启" : "关闭", args.LinkedClientCount);
            var toAdd = new Util.Model.ConsoleData(args.ConsoleMsg, (Util.Model.ConsoleMsgType)args.ConsoleMsgType);

            frm.Dispatcher.Invoke(() =>
            {
                frm.ucConsole_Log.Add(toAdd);
            });
        }

        private string _IP = "192.168.1.215";
        public string IP
        {
            get
            {
                return _IP;
            }
            set
            {
                _IP = value;
                this.OnPropertyChanged("IP");
            }
        }

        private string _Port = "48001";
        public string Port
        {
            get
            {
                return _Port;
            }
            set
            {
                _Port = value;
                this.OnPropertyChanged("Port");
            }
        }

        private string _ServerInfo;
        public string ServerInfo
        {
            get { return _ServerInfo; }
            set
            {
                _ServerInfo = value;
                this.OnPropertyChanged("ServerInfo");
            }
        }


        public bool BtnStart_IsEnabled
        {
            get
            {
                bool r = true;
                // if(this.mMyTcpServer != null && this.mMyTcpServer.IsStart) // TODO 增加属性
                return r;
            }
        }

        public bool BtnStop_IsEnabled
        {
            get
            {
                bool r = true;
                // if(this.mMyTcpServer != null && this.mMyTcpServer.IsStart) // TODO 增加属性
                return r;
            }
        }

        public bool BtnSend_IsEnabled
        {
            get
            {
                bool r = true;

                return r;
            }
        }


        private bool _IsStandardReceive;

        public bool IsStandardReceive
        {
            get { return _IsStandardReceive; }
            set
            {
                bool temp = value;
                if (mMyTcpClient != null && mMyTcpClient.mIsStandardReceive != temp)
                {
                    mMyTcpClient.mIsStandardReceive = value;
                    _IsStandardReceive = value;
                }
                this.OnPropertyChanged("IsStandardReceive");
            }
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

        private List<Encoding> _EncodingList = new List<Encoding>() { Encoding.UTF8, Encoding.Unicode, Encoding.GetEncoding("GB2312"), Encoding.GetEncoding("GB18030") };

        public List<Encoding> EncodingList
        {
            get
            {
                return _EncodingList;
            }
        }

        private Encoding _SendEncoding = Encoding.UTF8;
        public Encoding SendEncoding
        {
            get { return _SendEncoding; }
            set
            {
                if (mMyTcpClient != null && mMyTcpClient.mSendEncoding != value)
                {
                    mMyTcpClient.mSendEncoding = value;

                    _SendEncoding = value;
                    this.OnPropertyChanged("SendEncoding");
                }
            }
        }

        private Encoding _ReceiveEncoding = Encoding.UTF8;
        public Encoding ReceiveEncoding
        {
            get { return _ReceiveEncoding; }
            set
            {
                if (mMyTcpClient != null && mMyTcpClient.mReceiveEncoding != value)
                {
                    mMyTcpClient.mReceiveEncoding = value;

                    _ReceiveEncoding = value;
                    this.OnPropertyChanged("ReceiveEncoding");
                }
            }
        }
    }
}
