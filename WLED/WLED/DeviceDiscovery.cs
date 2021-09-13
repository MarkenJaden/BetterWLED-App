using System;
using Tmds.MDns;

namespace WLED
{
    //Discover _http._tcp services via mDNS/Zeroconf and verify they are WLED devices by sending an API call
    class DeviceDiscovery
    {
        private static DeviceDiscovery _instance;
        private readonly ServiceBrowser serviceBrowser;
        public event EventHandler<DeviceCreatedEventArgs> ValidDeviceFound;

        private DeviceDiscovery()
        {
            serviceBrowser = new ServiceBrowser();
            serviceBrowser.ServiceAdded += OnServiceAdded;
        }

        public void StartDiscovery()
        {
            serviceBrowser.StartBrowse("_http._tcp");
        }

        public void StopDiscovery()
        {
            serviceBrowser.StopBrowse();
        }

        private async void OnServiceAdded(object sender, ServiceAnnouncementEventArgs e)
        {
            WLEDDevice toAdd = new WLEDDevice();
            foreach (var addr in e.Announcement.Addresses)
            {
                toAdd.NetworkAddress = addr.ToString(); break; //only get first address
            }
            toAdd.Name = e.Announcement.Hostname;
            toAdd.NameIsCustom = false;
            if (await toAdd.Refresh()) //check if the service is a valid WLED light
            {
                OnValidDeviceFound(new DeviceCreatedEventArgs(toAdd, false));
            }
        }

        public static DeviceDiscovery GetInstance()
        {
            return _instance ?? (_instance = new DeviceDiscovery());
        }

        protected virtual void OnValidDeviceFound(DeviceCreatedEventArgs e)
        {
            ValidDeviceFound?.Invoke(this, e);
        }
    }
}
