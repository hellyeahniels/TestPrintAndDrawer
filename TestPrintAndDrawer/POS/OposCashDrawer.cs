using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TestPrintAndDrawer.Helpers;
using TestPrintAndDrawer.Interfaces;
using Windows.Devices.Enumeration;
using Windows.Devices.PointOfService;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Xaml.Media;

namespace TestPrintAndDrawer.POS
{
    public class OposCashDrawer : IOposCashDrawer
    {
        internal CashDrawer drawer = null;
        internal ClaimedCashDrawer claimedDrawer = null;

        internal event Action StateChanged;

        private string _idCashDrawer;
        public string IDCashDrawer
        {
            get { return _idCashDrawer; }
            set { _idCashDrawer = value; }
        }

        private string _errorReason;
        public string ErrorReason
        {
            get { return _errorReason; }
            set { _errorReason = value; }
        }

        private bool _drawerFound;
        public bool DrawerFound
        {
            get { return _drawerFound; }
            set { _drawerFound = value; }
        }

        private bool _isBusyFindingDrawer;
        public bool IsBusyFindingDrawer
        {
            get { return _isBusyFindingDrawer; }
            set { _isBusyFindingDrawer = value; }
        }

        public event EventHandler DrawerClosed;

        public async Task FindAndClaimCashDrawerAsync(string cashdrawerId)
        {
            _errorReason = "";
            ReleaseAllCashDrawers();

            if (await CreateDefaultCashDrawerObject(cashdrawerId))
            {
                if (await ClaimCashDrawer())
                {
                    if (await EnableCashDrawer())
                    {
                        //drawer.StatusUpdated += drawer_StatusUpdated;
                        _idCashDrawer = claimedDrawer.DeviceId;
                    }
                    else
                    {
                        _errorReason = "Failed to enable cash drawer.";
                    }
                }
                else
                {
                    _errorReason = "Failed to claim cash drawer.";
                }
            }
            else
            {
                _errorReason = "Cash drawer not found. Please connect a cash drawer.";
            }
            return;
        }

        public async Task Init(string id)
        {
            _idCashDrawer = id;
            await FindAndClaimCashDrawerAsync(_idCashDrawer);
            // check if drawer is enabled
            //if (claimedDrawer != null && !claimedDrawer.IsEnabled)
            //{
            //    await FindAndClaimCashDrawerAsync(_idCashDrawer);
            //    // check again
            //    if (claimedDrawer != null && !claimedDrawer.IsEnabled)
            //    {
            //        await FindAndClaimCashDrawerAsync(""); // try without id
            //        if (claimedDrawer != null && !claimedDrawer.IsEnabled)
            //        {
            //            await FindAndClaimCashDrawerAsync(""); // last try
            //        }
            //    }
            //}
            if (claimedDrawer != null)
                _drawerFound = true;
            else
                _drawerFound = false;
        }

        public void ReleaseClaimedCashDrawer()
        {
            if (claimedDrawer != null)
            {
                claimedDrawer.Dispose();
                claimedDrawer = null;
                StateChanged?.Invoke();
            }
        }

        public void ReleaseAllCashDrawers()
        {
            ReleaseClaimedCashDrawer();

            if (drawer != null)
            {
                drawer.Dispose();
                drawer = null;
                StateChanged?.Invoke();
            }
        }


        /// <summary>
        /// Creates the default cash drawer.
        /// </summary>
        /// <returns>True if the cash drawer was created, false otherwise.</returns>
        private async Task<bool> CreateDefaultCashDrawerObject(string cashdrawerId)
        {
            if (drawer == null)
            {
                if (string.IsNullOrWhiteSpace(cashdrawerId))
                    drawer = await DeviceHelpers.GetFirstCashDrawerAsync();
                else
                    drawer = await DeviceHelpers.GetCashDrawerById(cashdrawerId);
                if (drawer == null)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Attempt to claim the connected cash drawer.
        /// </summary>
        /// <returns>True if the cash drawer was successfully claimed, false otherwise.</returns>
        private async Task<bool> ClaimCashDrawer()
        {
            if (drawer == null)
                return false;

            if (claimedDrawer == null)
            {
                claimedDrawer = await drawer.ClaimDrawerAsync();
                if (claimedDrawer == null)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Attempt to enabled the claimed cash drawer.
        /// </summary>
        /// <returns>True if the cash drawer was successfully enabled, false otherwise.</returns>
        private async Task<bool> EnableCashDrawer()
        {
            if (claimedDrawer == null)
                return false;

            if (claimedDrawer.IsEnabled)
                return true;

            return await claimedDrawer.EnableAsync();
        }

        /// <summary>
        /// Attempt to open the claimed cash drawer.
        /// </summary>
        /// <returns>True if the cash drawer was successfully opened, false otherwise.</returns>
        public async Task<bool> OpenCashDrawer()
        {
            _errorReason = "";
            if (claimedDrawer == null || !claimedDrawer.IsEnabled)
            {
                _errorReason = "Cash drawer open failed.";
                return false;
            }

            return await claimedDrawer.OpenDrawerAsync();
        }

    }
}
