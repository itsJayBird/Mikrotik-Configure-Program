using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for SecondaryConfigure.xaml
    /// </summary>
    public partial class SecondaryConfigure : Page
    {
        private readonly BackgroundWorker worker = new BackgroundWorker { WorkerReportsProgress = true };
        private RouterInfo routerinfo = new RouterInfo();
        private bool success = false;
        public SecondaryConfigure(object routerinfo)
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
            string secondaryIP = "";
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                customerName = this.q1.Text.Trim();
                wifiName = this.q2.Text.Trim();
                wifiPassword = this.q3.Text.Trim();
                secondaryIP = this.q4.Text.Trim();
            });

            if (wifiPassword.Length < 8)
            {
                MessageBox.Show("Password must be longer than 8 characters!");
                (sender as BackgroundWorker).ReportProgress(0);
                success = false;
            }
            else
            {
                // CREATE THE CONTROLLERS
                Controller controller = new Controller();
                (sender as BackgroundWorker).ReportProgress(25);

                // USE THE UTILITY FUNCTION TO GET MODEL INFORMATION
                string routerModel = controller.getRouterModel(routerinfo);
                routerinfo.setModel(routerModel);
                (sender as BackgroundWorker).ReportProgress(50);

                // run script
                controller.executeConfiguration(customerName, wifiName, wifiPassword, secondaryIP, routerinfo);
                MessageBox.Show($"Configuration set!");
                (sender as BackgroundWorker).ReportProgress(100);

                success = true;
            }



        }
        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (success)
            {
                // TO CONFIRM EXIT
                Dispatcher.Invoke((Action)delegate
                {
                    StartUp c = new StartUp();
                    this.NavigationService.Navigate(c);
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
