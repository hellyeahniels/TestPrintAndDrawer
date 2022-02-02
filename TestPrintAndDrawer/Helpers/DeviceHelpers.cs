using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.PointOfService;

namespace TestPrintAndDrawer.Helpers
{
    public partial class DeviceHelpers
    {
        // By default, use all connections types.
        public static async Task<CashDrawer> GetFirstCashDrawerAsync(PosConnectionTypes connectionTypes = PosConnectionTypes.All)
        {
            return await DeviceHelpers.GetFirstDeviceAsync(CashDrawer.GetDeviceSelector(connectionTypes), async (id) => await CashDrawer.FromIdAsync(id));
        }

        public static async Task<PosPrinter> GetFirstPosPrinterAsync(PosConnectionTypes connectionTypes = PosConnectionTypes.All)
        {
            return await DeviceHelpers.GetFirstDeviceAsync(PosPrinter.GetDeviceSelector(connectionTypes), async (id) => await PosPrinter.FromIdAsync(id));
        }

        public static async Task<CashDrawer> GetCashDrawerById(string cashdrawerId)
        {
            return await CashDrawer.FromIdAsync(cashdrawerId);
        }

        public static async Task<PosPrinter> GetPosPrinterById(string printerId)
        {
            return await PosPrinter.FromIdAsync(printerId);
        }

        // We use a DeviceWatcher instead of DeviceInformation.FindAllAsync because
        // the DeviceWatcher will let us see the devices as they are discovered,
        // whereas FindAllAsync returns results only after discovery is complete.

        public static async Task<T> GetFirstDeviceAsync<T>(string selector, Func<string, Task<T>> convertAsync)
            where T : class
        {
            var completionSource = new TaskCompletionSource<T>();
            var pendingTasks = new List<Task>();
            DeviceWatcher watcher = DeviceInformation.CreateWatcher(selector);

            watcher.Added += (DeviceWatcher sender, DeviceInformation device) =>
            {
                Func<string, Task> lambda = async (id) =>
                {
                    T t = await convertAsync(id);
                    if (t != null)
                    {
                        completionSource.TrySetResult(t);
                    }
                };
                pendingTasks.Add(lambda(device.Id));
            };

            watcher.EnumerationCompleted += async (DeviceWatcher sender, object args) =>
            {
                // Wait for completion of all the tasks we created in our "Added" event handler.
                await Task.WhenAll(pendingTasks);
                // This sets the result to "null" if no task was able to produce a device.
                completionSource.TrySetResult(null);
            };

            watcher.Removed += (DeviceWatcher sender, DeviceInformationUpdate args) =>
            {
                // We don't do anything here, but this event needs to be handled to enable realtime updates.
                // See https://aka.ms/devicewatcher_added.
            };

            watcher.Updated += (DeviceWatcher sender, DeviceInformationUpdate args) =>
            {
                // We don't do anything here, but this event needs to be handled to enable realtime updates.
                // See https://aka.ms/devicewatcher_added.
            };

            watcher.Start();

            // Wait for enumeration to complete or for a device to be found.
            T result = await completionSource.Task;

            watcher.Stop();

            return result;
        }
    }
}
