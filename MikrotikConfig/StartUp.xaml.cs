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
        Controller controller = new Controller();
        string[] files = { "\\log.rsc", "\\info.txt", "\\ros.npk", "\\ipFile.txt" };
        public StartUp()
        { 
            
            string path = Directory.GetCurrentDirectory();
            foreach (string f in files)
            {
                string filepath = path + f;
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                }
            }
            InitializeComponent();
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            controller.scriptHeader();
            

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
            (sender as BackgroundWorker).ReportProgress(25);
            var decision = controller.initiateConfiguration();
            (sender as BackgroundWorker).ReportProgress(50);

            if (decision.Item1 == false)
            {
                if (decision.Item2 == 1)
                {
                    (sender as BackgroundWorker).ReportProgress(0);
                    // socket exception
                    MessageBox.Show("WARNING!\nRouter is refusing connection" +
                                    " on port 22!\nYou might need to manually " +
                                    "reset this router to continue using this program.");
                }
                if (decision.Item2 == 2)
                {
                    (sender as BackgroundWorker).ReportProgress(100);
                    // authentication exception
                    // FIRST GET THE CREDENTIALS
                    RouterUtility ru = new RouterUtility();
                    string[] cred = ru.getCredentials();
                    RouterInfo ri = new RouterInfo(host, cred[0], cred[1]);
                    ri.setFileName(controller.name);

                    // THEN GO TO THE PRECONFIGURED SELECTION
                    Dispatcher.Invoke((Action)delegate
                    {
                        PreconfiguredRouterSelection pr = new PreconfiguredRouterSelection(ri);
                        NavigationService.Navigate(pr);
                    });
                    
                
                }
            }

            if (decision.Item1 == true)
            {
                (sender as BackgroundWorker).ReportProgress(100);
                // get default credentials first
                var info = controller.getDefaultCredentials();
                info.setFileName(controller.name);
                Dispatcher.Invoke((Action)delegate
                {
                    NewRouterSelection newRouter = new NewRouterSelection(info);
                    //open newRouterSelection Page
                    NavigationService.Navigate(newRouter);
                });
                
            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }
    }
}
