using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TestPrintAndDrawer.Interfaces
{
    public interface IOposCashDrawer
    {
        event EventHandler DrawerClosed;

        string IDCashDrawer
        {
            get;
            set;
        }

        string ErrorReason
        {
            get;
            set;
        }

        bool DrawerFound
        {
            get;
            set;
        }

        bool IsBusyFindingDrawer
        {
            get;
            set;
        }

        Task Init(string id);
        Task FindAndClaimCashDrawerAsync(string cashdrawerId);
        void ReleaseAllCashDrawers();
        Task<bool> OpenCashDrawer();
    }
}
