using Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace SocketServer
{
    /// <summary>
    /// FrmServer.xaml 的交互逻辑
    /// </summary>
    public partial class FrmServer : Window
    {
        FrmServer_ViewModel ViewModel { get; set; }
        public FrmServer()
        {
            InitializeComponent();
            this.Title = $"{this.Title} - V {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
            this.ViewModel = new FrmServer_ViewModel(this);
            this.DataContext = this.ViewModel;
        }
    }

    public class FrmServer_ViewModel : Client.ViewModel.BaseViewModel
    {
        FrmServer frm { get; set; }

        Util.Web.MyTcpServer mMyTcpServer { get; set; }

        public Command CMD_StartServer { get; set; }

        public Command CMD_StopServer { get; set; }

        public Command CMD_Send { get; set; }

        public Command CMD_StandardSend { get; set; }


        public FrmServer_ViewModel(FrmServer frm)
        {
            this.frm = frm;

            CMD_StartServer = new Command(() =>
            {
                mMyTcpServer.StartServer(this.IP, this.Port); 
                updateUI();
            });

            CMD_StopServer = new Command(() =>
            {
                mMyTcpServer.StopServer(); 
                updateUI();
            });

            CMD_Send = new Command(() =>
            {
                if (this.SendContent.IsNullOrWhiteSpace() == false)
                {
                    mMyTcpServer.Send(this.SendContent);
                }
                updateUI();
            });

            CMD_StandardSend = new Command(() =>
            {
                if (this.SendContent.IsNullOrWhiteSpace() == false)
                {
                    mMyTcpServer.StandardSend(this.SendContent);
                }
                updateUI();
            });

            mMyTcpServer = new Util.Web.MyTcpServer();
            mMyTcpServer.ReceiveText += new EventHandler<TcpXxxEventArgs>(receiveText_Handle);
            mMyTcpServer.StatusChange += new EventHandler<TcpXxxStatusChangeEventArgs>(tcpServer_StatusChange);
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
            var toAdd = new Util.Model.ConsoleData($"{args.Msg}", Util.Model.ConsoleMsgType.DEFAULT, args.EntryTime);

            frm.Dispatcher.Invoke(() =>
            {
                frm.ucConsole_ReceiveInfos.Add(toAdd);
            });
        }

        private void tcpServer_StatusChange(object sender, TcpXxxStatusChangeEventArgs args)
        {
            ServerInfo = string.Format("服务器{0}中, 正在连接共 {1} 个客户端", args.IsConnect ? "开启" : "关闭", args.LinkedClientCount);
            var toAdd = new Util.Model.ConsoleData(args.ConsoleMsg, (Util.Model.ConsoleMsgType)args.ConsoleMsgType, args.EntryTime);

            frm.Dispatcher.Invoke(() =>
            {
                frm.ucConsole_Log.Add(toAdd);
                updateUI();
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
                return this.mMyTcpServer.IsServerStart == false;
            }
        }

        public bool BtnStop_IsEnabled
        {
            get
            {
                return this.mMyTcpServer.IsServerStart == true;
            }
        }

        public bool BtnSend_IsEnabled
        {
            get
            {
                return this.mMyTcpServer.IsServerStart == true;
            }
        }

        private void updateUI()
        {
            this.OnPropertyChanged("BtnStart_IsEnabled");
            this.OnPropertyChanged("BtnStop_IsEnabled");
            this.OnPropertyChanged("BtnSend_IsEnabled");
        }

        private bool _IsStandardReceive;

        public bool IsStandardReceive
        {
            get { return _IsStandardReceive; }
            set
            {
                bool temp = value;
                if (mMyTcpServer != null && mMyTcpServer.mIsStandardReceive != temp)
                {
                    mMyTcpServer.mIsStandardReceive = value;
                    _IsStandardReceive = value;
                }
                this.OnPropertyChanged("IsStandardReceive");
            }
        }

        #region 接收发送编码

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
                if (mMyTcpServer != null && mMyTcpServer.mSendEncoding != value)
                {
                    mMyTcpServer.mSendEncoding = value;

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
                if (mMyTcpServer != null && mMyTcpServer.mReceiveEncoding != value)
                {
                    mMyTcpServer.mReceiveEncoding = value;

                    _ReceiveEncoding = value;
                    this.OnPropertyChanged("ReceiveEncoding");
                }
            }
        }

        #endregion
    }
}
