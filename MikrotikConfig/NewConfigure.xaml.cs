using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MikrotikConfig
{
    /// <summary>
    /// Interaction logic for NewConfigure.xaml
    /// </summary>
    public partial class NewConfigure : Page
    {
        private readonly BackgroundWorker worker = new BackgroundWorker { WorkerReportsProgress = true };
        private RouterInfo routerinfo = new RouterInfo();
        public NewConfigure(object routerinfo)
        {
            InitializeComponent();
            this.routerinfo = (RouterInfo)routerinfo;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
        }
        private void configureClick(object sender, RoutedEventArgs e)
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
            //gather information for the controller
            string customerName = "";
            string wifiName = "";
            string wifiPassword = "";
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                customerName = this.q1.Text.Trim();
                wifiName = this.q2.Text.Trim();
                wifiPassword = this.q3.Text.Trim();
            });

            if (wifiPassword.Length < 8)
            {
                MessageBox.Show("Password must be longer than 8 characters!");
                (sender as BackgroundWorker).ReportProgress(0);
            }
            else
            {
                // CREATE THE CONTROLLER
                Controller controller = new Controller();
                (sender as BackgroundWorker).ReportProgress(25);
                Thread.Sleep(100);

                // USE THE UTILITY FUNCTION TO GET MODEL INFORMATION
                string routerModel = controller.getRouterModel(routerinfo);
                routerinfo.setModel(routerModel);
                (sender as BackgroundWorker).ReportProgress(66);
                Thread.Sleep(100);

                // CONFIGURE ROUTER
                controller.executeConfiguration(customerName, wifiName, wifiPassword, routerinfo);
                (sender as BackgroundWorker).ReportProgress(100);

                // message
                MessageBox.Show("Configuration set!");
            }


        }
        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // ONCE COMPLETED GO TO THE FINAL CHECK
            if (pbStatus.Value == 100)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    StartUp s = new StartUp();
                    this.NavigationService.Navigate(s);
                });
            }
        }
        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pbStatus.Value = e.ProgressPercentage;
        }

        private void cancelButton(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                StartUp s = new StartUp();
                this.NavigationService.Navigate(s);
            });
        }
    }
}
