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
        Controller controller = new Controller();
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
            // initialize router and grab default credentials
            (sender as BackgroundWorker).ReportProgress(25);
            var routerinfo = controller.initializeRouter();
            (sender as BackgroundWorker).ReportProgress(50);

            // if no connection
            if (routerinfo.password == null)
            {
                (sender as BackgroundWorker).ReportProgress(0);
                MessageBox.Show("Unable to connect to router!");
            }

            // if password is blank go to new router selection
            if (routerinfo.password == "")
            {
                (sender as BackgroundWorker).ReportProgress(100);
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    NewRouterSelection nrs = new NewRouterSelection(routerinfo);
                    this.NavigationService.Navigate(nrs);
                });
            }

            // if password is anything else go to preconfigured router
            if (routerinfo.password.Length > 0)
            {
                (sender as BackgroundWorker).ReportProgress(100);
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    PreconfiguredRouterSelection prs = new PreconfiguredRouterSelection(routerinfo);
                    this.NavigationService.Navigate(prs);
                });
            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }
    }
}
