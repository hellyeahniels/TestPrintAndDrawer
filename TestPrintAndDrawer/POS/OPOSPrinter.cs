using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TestPrintAndDrawer.Interfaces;
using Windows.Devices.Enumeration;
using Windows.Devices.PointOfService;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Xaml.Media;

namespace TestPrintAndDrawer.POS
{
    public class OPOSPrinter : IOPOSPrinter
    {
        internal PosPrinter CurrentPrinter = null;
        internal ClaimedPosPrinter ClaimedPrinter = null;
        internal DeviceInformation DeviceInfo = null;
        internal bool IsAnImportantTransaction = true;
        internal event Action StateChanged;

        private string _idPrinter;

        private int columns = 20;

        public string IDPrinter
        {
            get { return _idPrinter; }
            set { _idPrinter = value; }
        }

        private string _errorReason;
        public string ErrorReason
        {
            get { return _errorReason; }
            set { _errorReason = value; }
        }

        private Dictionary<string, string> _printerList;
        public Dictionary<string, string> PrinterList
        {
            get
            {
                if (_printerList == null)
                {
                    _printerList = new Dictionary<string, string>();
                }
                return _printerList;
            }
            set { _printerList = value; }
        }

        private bool _isBusyFindingPrinters;
        public bool IsBusyFindingPrinters
        {
            get { return _isBusyFindingPrinters; }
            set { _isBusyFindingPrinters = value; }
        }

        public void SubscribeToReleaseDeviceRequested()
        {
            ClaimedPrinter.ReleaseDeviceRequested += ClaimedPrinter_ReleaseDeviceRequested;
        }

        public void ReleaseClaimedPrinter()
        {
            if (ClaimedPrinter != null)
            {
                ClaimedPrinter.ReleaseDeviceRequested -= ClaimedPrinter_ReleaseDeviceRequested;
                ClaimedPrinter.Dispose();
                ClaimedPrinter = null;
                StateChanged?.Invoke();
            }
        }

        public void ReleaseAllPrinters()
        {
            ReleaseClaimedPrinter();

            if (CurrentPrinter != null)
            {
                CurrentPrinter.Dispose();
                CurrentPrinter = null;
                StateChanged?.Invoke();
            }
        }

        /// <summary>
        /// If the "Retain device" checkbox is checked, we retain the device.
        /// Otherwise, we allow the other claimant to claim the device.
        /// </summary>
        private async void ClaimedPrinter_ReleaseDeviceRequested(ClaimedPosPrinter sender, PosPrinterReleaseDeviceRequestedEventArgs args)
        {
            if (IsAnImportantTransaction)
            {
                await sender.RetainDeviceAsync();
            }
            else
            {
                ReleaseClaimedPrinter();
            }
        }

        bool IsPrinterClaimed()
        {
            if (ClaimedPrinter != null)
            {
                return true;
            }
            else
            {
                _errorReason = "Printer not found";
                return false;
            }
        }

        //public async void Init(string id)
        public async Task Init(string id)
        {
            _idPrinter = id;
            await FindAndClaimPrinterAsync();
        }

        public void SetNumberOfColumns(int cols)
        {
            if (CurrentPrinter != null && ClaimedPrinter != null)
                ClaimedPrinter.Receipt.CharactersPerLine = (uint)cols;
            columns = cols;
        }

        public List<int> GetNumberOfColumnsAvailable()
        {
            if (CurrentPrinter != null)
                return CurrentPrinter.Capabilities.Receipt.SupportedCharactersPerLine.Select(x => Convert.ToInt32(x)).ToList();
            else
            {
                //return null;
                List<int> result = new List<int>();
                result.Add(10);
                result.Add(20);
                result.Add(30);
                return result;
            }
        }

        public int GetCurrentNumberOfColumns()
        {
            if (CurrentPrinter != null && ClaimedPrinter != null)
                return Convert.ToInt32(ClaimedPrinter.Receipt.CharactersPerLine);
            else
                return 20;
        }

        // Cut the paper after printing enough blank lines to clear the paper cutter.
        private void PrintLineFeed(ReceiptPrintJob job, string receipt)
        {
            //PosPrinterPrintOptions po = new PosPrinterPrintOptions();
            //po.TypeFace = "Arial";
            //po.Bold = true;
            //po.Italic = true;
            //po.TypeFace = "serif";
            //job.Print(receipt + feedString, po);
            //job.Print(receipt, po);
            job.Print(receipt);
        }

        private void CutPaper(ReceiptPrintJob job)
        {
            if (CurrentPrinter.Capabilities.Receipt.CanCutPaper)
            {
                job.CutPaper();
            }
        }

        async Task<bool> ExecuteJobAndReportResultAsync(ReceiptPrintJob job)
        {
            bool success = await job.ExecuteAsync();
            if (!success)
            {

                ClaimedReceiptPrinter receiptPrinter = ClaimedPrinter.Receipt;
                if (receiptPrinter.IsCartridgeEmpty)
                {
                    _errorReason = "Printer is out of ink. Please replace cartridge.";
                }
                else if (receiptPrinter.IsCartridgeRemoved)
                {
                    _errorReason = "Printer cartridge is missing. Please replace cartridge.";
                }
                else if (receiptPrinter.IsCoverOpen)
                {
                    _errorReason = "Printer cover is open. Please close it.";
                }
                else if (receiptPrinter.IsHeadCleaning)
                {
                    _errorReason = "Printer is currently cleaning the cartridge. Please wait until cleaning has completed.";
                }
                else if (receiptPrinter.IsPaperEmpty)
                {
                    _errorReason = "Printer is out of paper. Please insert a new roll.";
                }
                else
                {
                    _errorReason = "Unable to print.";
                }
            }
            return success;
        }

        public async Task FillPosPrinterList(bool forceResfreshList)
        {
            try
            {
                if (PrinterList.Count == 0 || forceResfreshList)
                {
                    if (!_isBusyFindingPrinters)
                    {
                        _isBusyFindingPrinters = true;
                        DeviceInformationCollection deviceCollection = await DeviceInformation.FindAllAsync(PosPrinter.GetDeviceSelector()); // await duurt even
                        PrinterList.Clear();

                        foreach (DeviceInformation devInfo in deviceCollection)
                        {
                            PrinterList.Add(devInfo.Name, devInfo.Id);
                        }
                    }
                }
                _isBusyFindingPrinters = false;
            }
            catch (Exception)
            {
                _isBusyFindingPrinters = false;
                throw;
            }
        }

        public async Task FindAndClaimPrinterAsync()
        {
            _errorReason = "";
            bool bContinue = false;
            ReleaseAllPrinters();

            DeviceInformation deviceInfo = await DeviceInformation.CreateFromIdAsync(_idPrinter);

            DeviceInfo = deviceInfo;

            PosPrinter printer = null;
            if (deviceInfo != null)
            {
                try
                {
                    printer = await PosPrinter.FromIdAsync(deviceInfo.Id);
                }
                catch (Exception e)
                {
                    _errorReason = e.Message;
                }
            }
            if (printer != null && printer.Capabilities.Receipt.IsPrinterPresent)
            {
                CurrentPrinter = printer;
                bContinue = true;
            }
            else
            {
                // Get rid of the printer we can't use.
                printer?.Dispose();
                _errorReason = "Printer not present.";
                bContinue = false;
            }

            if (bContinue)
            {
                ClaimedPrinter = await CurrentPrinter.ClaimPrinterAsync();
                if (ClaimedPrinter == null)
                {
                    return;
                }
                else
                {
                    // Register for the ReleaseDeviceRequested event so we know when somebody
                    // wants to claim the printer away from us.
                    SubscribeToReleaseDeviceRequested();

                    if (await ClaimedPrinter.EnableAsync())
                    {
                        return;
                    }
                    else
                    {
                        ReleaseClaimedPrinter();
                        return;
                    }
                }
            }
            return;
        }

        public async Task Print(string text)
        {
            _errorReason = "";

            if (!IsPrinterClaimed())
            {
                return;
            }

            ReceiptPrintJob job = ClaimedPrinter.Receipt.CreateJob();

            // print Logo
            // BitmapFrame logoFrame = await LoadLogoBitmapAsync();
            // job.PrintBitmap(logoFrame, PosPrinterAlignment.Center);

            string[] lines = text.Split('\n');
            foreach (string line in lines)
            {
                if (line.StartsWith("<BARCODE:"))
                {
                    string[] strBarcodeArr = line.Split('>');
                    string[] strTyArr = strBarcodeArr[0].Split(':');
                    string barcode = strBarcodeArr[1];
                    string barcodetype = strTyArr[1];

                    switch (barcodetype.ToUpperInvariant())
                    {
                        case "EAN13":
                            job.PrintBarcode(barcode, BarcodeSymbologies.Ean13, 60, 2, PosPrinterBarcodeTextPosition.Below, PosPrinterAlignment.Center);
                            continue;
                        case "CODE128":
                            job.PrintBarcode(barcode, BarcodeSymbologies.Code128, 60, 2, PosPrinterBarcodeTextPosition.Below, PosPrinterAlignment.Center);
                            continue;
                    }
                }
                if (line.StartsWith("<IMAGE>"))
                {
                    string[] strImgArr = line.Split('>');
                    string imagepath = strImgArr[1];

                    BitmapFrame logoFrame = await LoadLogoBitmapAsync(imagepath);
                    job.PrintBitmap(logoFrame, PosPrinterAlignment.Center);
                    continue;
                }
                else if (line == "<NEWLINE>")
                {
                    PrintLineFeed(job, "\n");
                    continue;
                }
                else
                {
                    PrintLineFeed(job, line + "\n");
                    continue;
                }
            }

            // finally cut the receipt
            CutPaper(job);
            await ExecuteJobAndReportResultAsync(job);
        }

        public static async Task<BitmapFrame> LoadLogoBitmapAsync()
        {
            var uri = new Uri("ms-appx:///Assets/DigiPOS-logo-print.png");
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            using (var readStream = await file.OpenReadAsync())
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(readStream);
                return await decoder.GetFrameAsync(0);
            }
        }
        public static async Task<BitmapFrame> LoadLogoBitmapAsync(string imagePath)
        {
            //var uri = new Uri(imagePath);
            //StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            StorageFile file = await StorageFile.GetFileFromPathAsync(imagePath);
            using (var readStream = await file.OpenReadAsync())
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(readStream);
                return await decoder.GetFrameAsync(0);
            }
        }

    }
}
