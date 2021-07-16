using System.Windows;
using System.Windows.Controls;

namespace MikrotikConfig
{
    /// <summary>
    /// Interaction logic for NewRouterSelection.xaml
    /// </summary>
    public partial class NewRouterSelection : Page
    {
        private RouterInfo routerInfo = new RouterInfo();
        public NewRouterSelection(object routerInfo)
        {
            InitializeComponent();
            this.routerInfo = (RouterInfo)routerInfo;
        }

        private void mainRouter(object sender, RoutedEventArgs e)
        {
            // to new configure page
            NewConfigure c = new NewConfigure(routerInfo);
            this.NavigationService.Navigate(c);
        }

        private void asSecondary(object sender, RoutedEventArgs e)
        {
            // to new configure as secondary
            SecondaryConfigure s = new SecondaryConfigure(routerInfo);
            this.NavigationService.Navigate(s);
        }
    }
}
