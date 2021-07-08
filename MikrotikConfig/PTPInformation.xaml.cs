using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace MikrotikConfig
{
    /// <summary>
    /// Interaction logic for PTPInformation.xaml
    /// </summary>
    public partial class PTPInformation : Page
    {
        private List<string[]> rule = new List<string[]>();
        private readonly List<object> components = new List<object>();
        private readonly BackgroundWorker worker = new BackgroundWorker { WorkerReportsProgress = true };
        private int setupCount = 0;
        private RouterInfo routerinfo = new RouterInfo();
        public PTPInformation(object routerinfo)
        {
            InitializeComponent();
            this.routerinfo = (RouterInfo)routerinfo;
            components.Add(sta);
            components.Add(ap);
            components.Add(rtr);
            worker.ProgressChanged += worker_progressChanged;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            setupTextBox();
        }
        private void addClick(object sender, RoutedEventArgs e)
        {
            // check that a radio button has been clicked
            bool canContinue = checkButtons();

            if (canContinue == true)
            {
                // check what it is
                string buttonName = returnRadioButtonName();
                rule.Add(new string[] { addr.Text, buttonName });
                resetRadioButtons();
                resetTextBox();
                setupTextBox();
            }
            else
            {
                MessageBox.Show("Please select the type of device! This is necessary to ensure the correct ports are used!");
            }

        }

        private void removeClick(object sender, RoutedEventArgs e)
        {
            int a = 999;
            if (int.TryParse(removeNum.Text, out a) == true && a <= rule.Count || a > rule.Count)
            {
                this.rule.RemoveAt(a);
                resetTextBox();
                setupTextBox();

            }
            else
            {
                MessageBox.Show($"Please enter a valid rule number! {a} is not valid!");
            }
        }

        private void confirmClick(object sender, RoutedEventArgs e)
        {
            //make sure we add anything in the text field
            //string a = "";
            //foreach (string[] rules in rule)
            //{
            //    a += rules[0] + " " + rules[1] + "\n";
            //}
            //MessageBox.Show(a);
            if (addr.Text.Length > 1)
            {
                bool canContinue = checkButtons();

                if (canContinue == true)
                {
                    // check what it is
                    string buttonName = returnRadioButtonName();
                    rule.Add(new string[] { addr.Text, buttonName });
                    resetRadioButtons();
                    resetTextBox();
                    setupTextBox();
                }
                else
                {
                    MessageBox.Show("Please select the type of device! This is necessary to ensure the correct ports are used!");
                }
            }

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
            // CONTROLLERS
            RouterUtility ru = new RouterUtility();
            Controller controller = new Controller();
            (sender as BackgroundWorker).ReportProgress(33);

            // GET WAN IP FROM ROUTER, THIS WILL BE THE ADDRESS THE
            // RULES ARE FORWARDED TO
            string wanIP = ru.getWANIP(routerinfo.user, routerinfo.password);
            (sender as BackgroundWorker).ReportProgress(66);

            // finally send the ptp script over
            controller.setName(routerinfo.fileName);
            controller.ptpConfig(rule, wanIP);
            (sender as BackgroundWorker).ReportProgress(100);
            MessageBox.Show("PTP Configuration added successfully!");

            //back to main menu
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                ConfirmExit s = new ConfirmExit(routerinfo);
                NavigationService.Navigate(s);
            });

        }
        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }
        void worker_progressChanged(object sender, ProgressChangedEventArgs e)
        {
            pbStatus.Value = e.ProgressPercentage;
        }

        private string returnRadioButtonName()
        {
            string type = "";
            foreach (RadioButton button in components)
            {
                if (button.IsChecked == true)
                {
                    type = button.Content.ToString();
                    break;
                }
            }
            return type;
        }

        private void resetRadioButtons()
        {
            foreach (RadioButton button in components)
            {
                button.IsChecked = false;
            }
        }

        private void resetTextBox()
        {
            addr.Clear();
            rules.Clear();
            removeNum.Clear();
        }
        private void setupTextBox()
        {
            this.rules.Text += "Device\t\t\tIP\t\tRule #\n";
            int i = 0;
            //display all current rules
            foreach (string[] line in rule)
            {
                string tmp = "";
                if (line[1].Contains("Station"))
                {
                    tmp += $"{line[1]}\t\t\t{line[0]}\t{i}\n";
                }
                else
                {
                    tmp += $"{line[1]}\t\t{line[0]}\t{i}\n";
                }
                this.rules.Text += tmp;
                i++;
            }

            // first time run
            if (setupCount == 0)
            {
                this.rules.Text += "\"Example Rule\"\t\t1.1.1.1\t999";
            }

            setupCount++;
        }

        private bool checkButtons()
        {
            foreach (RadioButton button in components)
            {
                if (button.IsChecked == true)
                {
                    return true;
                }
            }
            return false;
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
