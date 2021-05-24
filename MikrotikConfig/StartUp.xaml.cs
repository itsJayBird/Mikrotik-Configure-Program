using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Renci.SshNet;
using System.IO;
using System.Threading;
using System.ComponentModel;

namespace MikrotikConfig
{
    /// <summary>
    /// Interaction logic for StartUp.xaml
    /// </summary>
    public partial class StartUp : Page
    {
        private readonly BackgroundWorker worker = new BackgroundWorker { WorkerReportsProgress = true };
        private readonly string host = "192.168.88.1";
        private readonly string username = "admin";
        private readonly string password = "";
        public StartUp()
        {
            InitializeComponent();
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
        }

        private void findRouterButton(object sender, RoutedEventArgs e)
        {
            if (worker.IsBusy != true)
            {
                worker.RunWorkerAsync();
            }
            else
            {
                worker_DoWork(null, null);
            }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool doNotShowMessage = false;
            (sender as BackgroundWorker).ReportProgress(25);
            // check if we can ssh via default credentials
            // create the client
            var client = new SshClient(host, username, password);
            
            //try connecting
            try
            {
                (sender as BackgroundWorker).ReportProgress(50);
                client.Connect();
            }
            // this is so we can catch a refused connection before we try to go any further
            catch (System.Net.Sockets.SocketException)
            {
                
            }
            // if it detects a password we move onto the main menu to configure new scripts
            catch (Renci.SshNet.Common.SshAuthenticationException)
            {
                doNotShowMessage = true;
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    (sender as BackgroundWorker).ReportProgress(100);
                    mainMenu a = new mainMenu();
                    NavigationService.Navigate(a);
                });
            }

            // if we are connected then we continue with the configuration
            if (client.IsConnected)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    // this opens newconfigure page
                    (sender as BackgroundWorker).ReportProgress(100);
                    Preconfigure config = new Preconfigure();
                    NavigationService.Navigate(config);
                });
            }
            if (!client.IsConnected && !doNotShowMessage)// if we are not connected, display interface IPs to check if we are on correct subnet
            {
                (sender as BackgroundWorker).ReportProgress(75);
                Utility util = new Utility();
                string list = util.getInterfaceIPs();
                MessageBox.Show("You are not connected!\nI have detected the following interfaces: " +
                                $"{list}\nPlease make sure you are on the correct subnet!");
                (sender as BackgroundWorker).ReportProgress(0);
            }
            client.Disconnect();
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }
    }
}
