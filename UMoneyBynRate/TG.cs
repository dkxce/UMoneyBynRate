////////////////////////////////////
// dkxce Tinkoff BYN Rate Grabber //
////////////////////////////////////

using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UMoneyBynRate
{
    public class TinkoffMoneyGrabber : RateGrabber, IRateGrabber
    {
        private List<string> AllowedTransfersCategories = new List<string>(new string[] { "CUTransfersPro", "CUTransfersPrivate" });

        public TinkoffMoneyGrabber()
        {
            name = "Tinkoff BYN Rate Grabber";
            url = "https://api.tinkoff.ru/v1/currency_rates?from=BYN&to=RUB";
        }

        public TinkoffMoneyGrabber(string url) : base(url)
        {
            name = "Tinkoff BYN Rate Grabber";
        }

        public override (double?, double?) GetRates(out Exception ex)
        {
            ex = null;
            HttpWebRequest wreq = (HttpWebRequest)HttpWebRequest.Create(url);
            wreq.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";
            string response = "";
            int result = 0;
            try
            {
                HttpWebResponse wres = (HttpWebResponse)wreq.GetResponse();
                Encoding enc = Encoding.UTF8;
                using (StreamReader streamReader = new StreamReader(wres.GetResponseStream(), enc))
                    response = streamReader.ReadToEnd();
                result = (int)wres.StatusCode;
            }
            catch (WebException e)
            {
                ex = e;
                HttpWebResponse wres = (HttpWebResponse)e.Response;
                Encoding enc = Encoding.UTF8;
                using (StreamReader streamReader = new StreamReader(wres.GetResponseStream(), enc))
                    response = streamReader.ReadToEnd();
                result = (int)wres.StatusCode;
            }
            catch (Exception e) { ex = e; };
            if (string.IsNullOrEmpty(response)) return (null, null);

            try
            {
                int iof = response.IndexOf("\"code\":933,\"name\":\"BYN\",\"strCode\":\"933\"");
                if (iof < 0) throw new Exception("Exchange rate not found");

                JObject jo = (JObject)JsonConvert.DeserializeObject(response);
                JArray ja = (JArray)jo["payload"]["rates"];
                double sellRate = double.MaxValue;
                double buyRate = double.MinValue;
                bool ok = false;
                foreach (JObject o in ja)
                {
                    string category = o["category"].Value<string>();
                    if (!AllowedTransfersCategories.Contains(category)) continue;
                    double sr = o["sell"].ToObject<double>();
                    double br = o["buy"].ToObject<double>();
                    if (sr < sellRate) sellRate = sr;
                    if (br > buyRate) buyRate = br;
                    ok = true;
                };
                if (ok) return (sellRate, buyRate);
            }
            catch (Exception e) { ex = e; };
            return (null, null);
        }
    }
}
