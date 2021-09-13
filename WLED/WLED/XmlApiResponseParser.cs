using System.Xml.Linq;
using Xamarin.Forms;

namespace WLED
{
    class XmlApiResponseParser
    {
        public static XmlApiResponse ParseApiResponse(string xml)
        {
            if (xml == null) return null;
            XmlApiResponse resp = new XmlApiResponse(); //XmlApiResponse object will contain parsed values
            try
            {
                XElement xe = XElement.Parse(xml);
                resp.Name = xe.Element("ds")?.Value ?? xe.Element("desc")?.Value;
                if (resp.Name == null) return null; //if we return at this point, parsing was unsuccessful (server likely not WLED device)

                string bri_s = xe.Element("ac")?.Value ?? xe.Element("act")?.Value;
                if (bri_s != null)
                {
                    int bri = 0;
                    int.TryParse(bri_s, out bri);
                    resp.Brightness = (byte)bri;
                    resp.IsOn = (bri > 0); //light is on if brightness > 0
                }

                double r = 0, g = 0, b = 0;
                int counter = 0;
                foreach (var el in xe.Elements("cl"))
                {
                    int co = 0;
                    int.TryParse(el?.Value, out co);
                    switch (counter)
                    {
                        case 0: r = co / 255.0; break;
                        case 1: g = co / 255.0; break;
                        case 2: b = co / 255.0; break;
                    }
                    counter++;
                }
                resp.LightColor = new Color(r, g, b);
                return resp;
            } catch
            {
                //Exceptions here indicate unsuccessful parsing
            }
            return null;
        }
    }
}
