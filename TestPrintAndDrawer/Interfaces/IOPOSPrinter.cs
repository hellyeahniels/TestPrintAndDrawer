using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TestPrintAndDrawer.Interfaces
{
    public interface IOPOSPrinter
    {
        string ErrorReason
        {
            get;
            set;
        }

        Dictionary<string, string> PrinterList
        {
            get;
            set;
        }

        bool IsBusyFindingPrinters
        {
            get;
            set;
        }

        //void Init(string id);
        Task Init(string id);
        void SetNumberOfColumns(int cols);
        List<int> GetNumberOfColumnsAvailable();
        int GetCurrentNumberOfColumns();
        Task FillPosPrinterList(bool forceResfreshList);
        //void FindAndClaimPrinterAsync();
        Task FindAndClaimPrinterAsync();
        void ReleaseAllPrinters();
        //Task<bool> FindAndClaimPrinterAsync();
        Task Print(string text);
        //Task Print2(string text);
    }
}
