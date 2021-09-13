using System;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace WLED
{
    //Viewmodel: Page for hiding and deleting existing device list entries
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DeviceModificationListViewPage : ContentPage
    {
        private readonly ObservableCollection<WLEDDevice> deviceList;
        public DeviceModificationListViewPage (ObservableCollection<WLEDDevice> items)
        {
            InitializeComponent();

            deviceList = items;
            DeviceModificationListView.ItemsSource = deviceList;
        }

        private void OnDeleteButtonTapped(object sender, EventArgs eventArgs)
        {
            Button s = sender as Button;
            if (!(s.Parent.BindingContext is WLEDDevice targetDevice)) return;

            deviceList.Remove(targetDevice);

            //Go back to main device list view if no devices in list
            if (deviceList.Count == 0) Navigation.PopModalAsync(false);
        }

        private void OnDeviceTapped(object sender, ItemTappedEventArgs e)
        {
            //Deselect Item immediately
            ((ListView)sender).SelectedItem = null;

            if (e.Item is WLEDDevice targetDevice)
            {
                //Toggle Device enabled (disabled = hidden in list)
                targetDevice.IsEnabled = !targetDevice.IsEnabled;
            }
        }
    }
}