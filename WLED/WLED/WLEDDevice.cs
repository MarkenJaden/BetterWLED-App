using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Xamarin.Forms;

namespace WLED
{
    enum DeviceStatus { Default, Unreachable, Error };

    //Data Model. Represents a WLED light with a network address, name, and some current light values.
    [XmlType("dev")]
    public class WLEDDevice : INotifyPropertyChanged, IComparable
    {
        private string networkAddress = "192.168.4.1";                          //device IP (can also be hostname if applicable)
        private string name = "";                                               //device display name ("Server Description")
        private DeviceStatus status = DeviceStatus.Default;                     //Current connection status
        private bool stateCurrent = false;                                      //Is the light currently on?
        private bool isEnabled = true;                                          //Disabled devices don't get polled or show up in the list
        private double brightnessReceived = 0.9, brightnessCurrent = 0.9;       //There are two vars for brightness to discern API responses from slider updates

        [XmlElement("url")]
        public string NetworkAddress
        {
            set
            {
                if (value == null || value.Length < 3) return; //More elaborate checking for URL syntax could be added here
                networkAddress = value;
            }
            get => networkAddress;
        }

        [XmlElement("name")]
        public string Name
        { 
            set
            {
                if (value == null || name.Equals(value)) return; //Make sure name is not set to null
                name = value;
                OnPropertyChanged("Name");
            }
            get => name;
        }

        internal DeviceStatus CurrentStatus
        {
            set
            {
                status = value;
                OnPropertyChanged("Status");
            }
            get => status;
        }

        [XmlElement("ncustom")]
        public bool NameIsCustom { get; set; } = true; //If the light name is custom, the name returned by the API response will be ignored

        [XmlElement("en")]
        public bool IsEnabled
        {
            set
            {
                isEnabled = value;
                OnPropertyChanged("Status");
                OnPropertyChanged("ListHeight");
                OnPropertyChanged("TextColor");
                OnPropertyChanged("IsEnabled");
            }
            get => isEnabled;
        }

        [XmlIgnore]
        public double BrightnessCurrent
        {
            set
            {
                brightnessCurrent = value;
                if (brightnessCurrent == brightnessReceived) return;
                byte toSend = (byte)Math.Round(brightnessCurrent);
                RateLimitedSender.SendAPICall(this, "A=" + toSend);
            }
            get => brightnessCurrent;
        }

        [XmlIgnore]
        public Color ColorCurrent { get; set; }

        [XmlIgnore]
        public bool StateCurrent
        {
            get => stateCurrent;
            set { OnPropertyChanged("StateColor"); stateCurrent = value; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        //helper properties for updating view dynamically via data binding
        [XmlIgnore]
        public Color StateColor => StateCurrent ? Color.FromHex("#666") : Color.FromHex("#222"); //button background color

        [XmlIgnore]
        public string ListHeight => isEnabled ? "-1" : "0"; //height of one view cell (set to 0 to hide device)

        [XmlIgnore]
        public string TextColor => isEnabled ? "#FFF" : "#999"; //text color for modification page

        [XmlIgnore]
        public string Status //string containing IP and current status, second label in list viewcell
        {
            get
            {
                string statusText = "";
                if (IsEnabled)
                {
                    switch (status)
                    {
                        case DeviceStatus.Default: statusText = ""; break;
                        case DeviceStatus.Unreachable: statusText = " (Offline)"; break;
                        case DeviceStatus.Error: statusText = " (Error)"; break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    statusText = " (Hidden)";
                }
                return $"{networkAddress}{statusText}";
            }
        }

        //constructors
        public WLEDDevice() { }

        public WLEDDevice(string nA, string name)
        {
            NetworkAddress = nA;
            Name = name;
        }

        //member functions

        //send a call to this device's WLED HTTP API
        //API reference: http://apiref.wled.me
        public async Task<bool> SendAPICall(string call)
        {
            string url = "http://" + networkAddress;
            if (networkAddress.StartsWith("https://"))
            {
                url = networkAddress;
            }

            string response = await DeviceHttpConnection.GetInstance().Send_WLED_API_Call(url, call);
            switch (response)
            {
                case null:
                    CurrentStatus = DeviceStatus.Unreachable;
                    return false;
                //404 or other non-success http status codes, indicates that target is not a WLED device
                case "err":
                    CurrentStatus = DeviceStatus.Error;
                    return false;
            }

            XmlApiResponse deviceResponse = XmlApiResponseParser.ParseApiResponse(response);
            if (deviceResponse == null) //could not parse XML API response
            {
                CurrentStatus = DeviceStatus.Error;
                return false;
            }

            CurrentStatus = DeviceStatus.Default; //the received response was valid

            if (!NameIsCustom) Name = deviceResponse.Name;

            //only consider brightness if light is on and if it wasn't modified in the same call (prevents brightness slider "jumps")
            if (deviceResponse.Brightness > 0 && !call.Contains("A="))
            {
                brightnessReceived = deviceResponse.Brightness;
                BrightnessCurrent = brightnessReceived;
                OnPropertyChanged("BrightnessCurrent"); //update slider binding
            }

            ColorCurrent = deviceResponse.LightColor;
            OnPropertyChanged("ColorCurrent");

            StateCurrent = deviceResponse.IsOn;
            return true;
        }

        public async Task<bool> Refresh() //fetches updated values from WLED device
        {
            if (!IsEnabled) return false;
            return await SendAPICall("");
        }

        public int CompareTo(object comp) //compares devices in alphabetic order based on name
        {
            WLEDDevice c = comp as WLEDDevice;
            if (c == null || c.Name == null) return 1;
            int result = (string.Compare(name, c.name, StringComparison.Ordinal));
            return result != 0 ? result : string.Compare(networkAddress, c.networkAddress, StringComparison.Ordinal);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
