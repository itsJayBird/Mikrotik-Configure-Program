using System;
using System.Windows;
using System.Windows.Controls;

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
            ru.resetConfig(routerinfo);

            MessageBox.Show("Router is being defaulted! Wait for it to come back online!");

            // EXIT APP WHEN DONE
            Dispatcher.Invoke((Action)delegate
            {
                ru.pingRouter(routerinfo);
                StartUp s = new StartUp();
                this.NavigationService.Navigate(s);
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
