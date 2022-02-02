using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TestPrintAndDrawer.POS;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TestPrintAndDrawer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        OPOSPrinter oposprinter = new OPOSPrinter();
        OposCashDrawer oposcashdrawer = new OposCashDrawer();

        string printerid = @"\\?\SWD#OPOS#POSPrinter:CT-E351_1#{c7bc9b22-21f0-4f0d-9bb6-66c229b8cd33}";
        string drawerid = @"\\?\SWD#OPOS#CashDrawer:CT-E351_1.CD1#{772e18f2-8925-4229-a5ac-6453cb482fda}";

        public MainPage()
        {
            this.InitializeComponent();

        }

        public async Task SetPrinterSettingsAsync()
        {
            if (!String.IsNullOrWhiteSpace(printerid))
            {
                await oposprinter.Init(printerid);
                oposprinter.SetNumberOfColumns(48);
                if (!String.IsNullOrWhiteSpace(oposprinter.ErrorReason))
                    errortext.Text = "Printer error: " + oposprinter.ErrorReason;
                else
                    errortext.Text = "Printer initialized";
            }

            if (!oposprinter.IsBusyFindingPrinters && oposprinter.PrinterList.Count == 0)
            {
                await oposprinter.FillPosPrinterList(false);
            }
        }

        protected async Task SetDrawerSettingsAsync()
        {
            if (string.IsNullOrWhiteSpace(drawerid))
            {
                await oposcashdrawer.Init("");
            }
            else
            {
                await oposcashdrawer.Init(drawerid);
            }

            if (oposcashdrawer.DrawerFound && !String.IsNullOrWhiteSpace(oposcashdrawer.ErrorReason))
                errortext.Text = "Drawer error: " + oposcashdrawer.ErrorReason;
            else
                errortext.Text = "Drawer initialized";
        }


        private async void printbutton_Click(object sender, RoutedEventArgs e)
        {
            if (oposprinter != null)
            {
                await oposprinter.Print("Line 1\nLine 2\nLine 3\nLine 4\n\n\n\n\n");
                if (!String.IsNullOrWhiteSpace(oposprinter.ErrorReason))
                    errortext.Text = "Printer error: " + oposprinter.ErrorReason;
                else
                    errortext.Text = "Print OK";
            }
            else
            {
                errortext.Text = "Printer error: No printer selected";
            }
        }

        private async void opendrawerbutton_Click(object sender, RoutedEventArgs e)
        {
            if (oposcashdrawer != null)
            {
                //await App.OposCashDrawer.Init("");
                await oposcashdrawer.OpenCashDrawer();
                if (!String.IsNullOrWhiteSpace(oposcashdrawer.ErrorReason))
                    errortext.Text = "Drawer error: " + oposcashdrawer.ErrorReason;
                else
                    errortext.Text = "Drawer opened";
            }
        }
        private void initprinterbutton_Click(object sender, RoutedEventArgs e)
        {
            _ = SetPrinterSettingsAsync();

            printbutton.IsEnabled = true;
        }

        private void initdrawerbutton_Click(object sender, RoutedEventArgs e)
        {
            _ = SetDrawerSettingsAsync();

            opendrawerbutton.IsEnabled = true;
        }

        private void releaseprinterbutton_Click(object sender, RoutedEventArgs e)
        {
            oposprinter.ReleaseAllPrinters();
            printbutton.IsEnabled = false;
            errortext.Text = "Printer released";
        }

        private void releasedrawerbutton_Click(object sender, RoutedEventArgs e)
        {
            oposcashdrawer.ReleaseAllCashDrawers();
            opendrawerbutton.IsEnabled = false;
            errortext.Text = "Drawer released";
        }
    }
}
