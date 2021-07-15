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
            string path = Directory.GetCurrentDirectory() + "\\ros.npk";
            if (File.Exists(path))
            {
                File.Delete(path);
            }

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

            // check if we need to update/upgrade
            if(routerinfo.upgradeChecks.Item2 == true && routerinfo.upgradeChecks.Item1 == false)
            {
                // method to set the upgrade script/scheduler in router
                controller.forceUpgrade(routerinfo);
            }

            (sender as BackgroundWorker).ReportProgress(33);
            if(routerinfo.upgradeChecks.Item1 == true)
            {
                controller.setUpgradeScript(routerinfo);
                MessageBox.Show($"Router firmware seems to be out of date!\nCurrently loaded firmware is {routerinfo.currentFW}, the accepted firmware is {routerinfo.masterFW}!\nRouter will reboot shortly, please wait for the second reboot!");
                (sender as BackgroundWorker).ReportProgress(50);
                // start upgrade process
                controller.updateRouter(routerinfo);
                (sender as BackgroundWorker).ReportProgress(66);
                // ping router afterwards and go back to main page
                controller.pingRouter(routerinfo);

                (sender as BackgroundWorker).ReportProgress(100);
                MessageBox.Show("Please wait for the router to comeback online and start again!");

                // exit app to reset everything
                Environment.Exit(0);
            }
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
