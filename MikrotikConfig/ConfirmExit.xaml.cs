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

namespace MikrotikConfig
{
    /// <summary>
    /// Interaction logic for ConfirmExit.xaml
    /// </summary>
    public partial class ConfirmExit : Page
    {
        RouterInfo routerinfo = new RouterInfo();
        public ConfirmExit(object routerinfo)
        {
            InitializeComponent();
            this.routerinfo = (RouterInfo)routerinfo;
        }

        private void addPTP(object sender, RoutedEventArgs e)
        {
            // TAKES YOU TO THE PTP PAGE
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                PTPInformation ptp = new PTPInformation(routerinfo);
                this.NavigationService.Navigate(ptp);
            });
        }

        private void resetRouter(object sender, RoutedEventArgs e)
        {
            // RUN THE RESET METHOD
            RouterUtility ru = new RouterUtility();
            ru.resetConfig(routerinfo.user, routerinfo.password);
            // EXIT TO MAIN MENU
            ru.pingRouter(routerinfo);
            Environment.Exit(0);
        }

        private void finishConfiguration(object sender, RoutedEventArgs e)
        {
            // USE THE CONTROLLER TO FINALIZE AND UPLOAD SCRIPT
            RouterUtility ru = new RouterUtility();
            string model = ru.getModel(routerinfo.host,routerinfo.user,routerinfo.password).Item1;
            Controller controller = new Controller();
            controller.setName(routerinfo.fileName);
            controller.uploadScript(routerinfo.user, routerinfo.password, model);

            // EXIT
            MessageBox.Show("Configuration complete! Wait for the mikrotik to come back online!");
            ru.pingRouter(routerinfo);
            Environment.Exit(0);
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
