using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Calculator_Foundation_Etabs_API
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnCheckConnection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ETABSv1.cHelper myHelper = new ETABSv1.Helper();
                ETABSv1.cOAPI myETABSObject = myHelper.GetObject("CSI.ETABS.API.ETABSObject");

                if (myETABSObject == null)
                {
                    StatusText.Text = "Status: Error - Could not find running ETABS instance.";
                    StatusText.Foreground = Brushes.Red;
                    return;
                }

                ETABSv1.cSapModel mySapModel = myETABSObject.SapModel;
                
                string version = "";
                double versionStatus = 0;
                mySapModel.GetVersion(ref version, ref versionStatus);

                StatusText.Text = $"Status: Connected to ETABS!\nVersion: {version} ({versionStatus})";
                StatusText.Foreground = Brushes.Green;

                MessageBox.Show("Hello World from ETABS API! Connection Successful.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Status: Error - {ex.Message}";
                StatusText.Foreground = Brushes.Red;
                MessageBox.Show($"Connection failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
