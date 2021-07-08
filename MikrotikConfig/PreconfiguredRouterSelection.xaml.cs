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
    /// Interaction logic for PreconfiguredRouterSelection.xaml
    /// </summary>
    public partial class PreconfiguredRouterSelection : Page
    {
        private RouterInfo routerinfo = new RouterInfo();
        public PreconfiguredRouterSelection(object routerinfo)
        {
            InitializeComponent();
            this.routerinfo = (RouterInfo)routerinfo;
        }
        private void reset_Click(object sender, RoutedEventArgs e)
        {
            // USE THE RESET METHOD TO RESET THE CONFIGURATION
            RouterUtility ru = new RouterUtility();
            ru.resetConfig(routerinfo.user, routerinfo.password);

            MessageBox.Show("Router is being defaulted! Wait for it to come back online!");

            // EXIT APP WHEN DONE
            Dispatcher.Invoke((Action)delegate
            {
                ru.pingRouter(routerinfo);
                Environment.Exit(0);
            });
        }

        private void ptp_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke((Action)delegate
            {
                PTPInformation ptp = new PTPInformation(routerinfo);
                this.NavigationService.Navigate(ptp);
            });
        }
    }
}
