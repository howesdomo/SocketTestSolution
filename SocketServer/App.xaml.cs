using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SocketServer
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            #region 注册事件 - 捕获未处理的异常

            // 主线程
            this.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);

            // 非主线程
            System.AppDomain.CurrentDomain.UnhandledException += new System.UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            #endregion

            base.OnStartup(e);
        }

        #region 捕获未处理的异常

        /// <summary>
        /// 主线程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                HandleException("DispatcherUnhandledException", e.Exception);
                e.Handled = true;
            }
            catch (Exception ex)
            {
#if DEUBG
                string msg = $"HandleException 发生异常。{ex.Message}";
                System.Diagnostics.Debug.WriteLine(msg);
                System.Diagnostics.Debugger.Break();
#endif
                throw ex;
            }
        }

        /// <summary>
        /// 非主线程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is System.Exception)
                {
                    HandleException("CurrentDomain.UnhandledException", e.ExceptionObject as System.Exception);
                }
            }
            catch (Exception ex)
            {
#if DEUBG
                string msg = $"HandleException 发生异常。{ex.Message}";
                System.Diagnostics.Debug.WriteLine(msg);
                System.Diagnostics.Debugger.Break();
#endif
                throw ex;
            }
        }

        public static void HandleException(string from, Exception ex)
        {
            Util.LogUtils.LogAsync
            (
                content: ex.GetFullInfo(),
                baseDirectory: System.IO.Path.Combine(Environment.CurrentDirectory, "crash")
            );

            MessageBox.Show
            (
                caption: "捕获到以下错误，请与管理员联系以获取帮助。",
                messageBoxText: $"在 {from} 捕获到以下错误\r\n{ex.GetFullInfo()}"
            );
        }

        #endregion
    }
}
