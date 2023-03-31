////////////////////////////////////
// dkxce Tinkoff BYN Rate Grabber //
////////////////////////////////////

using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UMoneyBynRate
{
    public class AlfabankMoneyGrabber : RateGrabber, IRateGrabber
    {
        private List<string> AllowedTransfersCategories = new List<string>(new string[] { "CUTransfersPro", "CUTransfersPrivate" });

        public AlfabankMoneyGrabber()
        {
            name = "Alfabank BYN Rate Grabber";
            url = "https://alfabank.ru/api/v1/scrooge/currencies/alfa-rates?currencyCode.in=BYN&rateType.in=rateCass,makeCash&lastActualForDate.eq=true&clientType.eq=standardCC&date.lte={date}";
        }

        public AlfabankMoneyGrabber(string url) : base(url)
        {
            name = "Alfabank BYN Rate Grabber";
        }

        public override (double?, double?) GetRates(out Exception ex)
        {
            ex = null;
            string curl = url.Replace("{date}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssK"));
            HttpWebRequest wreq = (HttpWebRequest)HttpWebRequest.Create(curl);
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
                int iof = response.IndexOf("\"currencyCode\":\"BYN\"");
                if (iof < 0) throw new Exception("Exchange rate not found");

                JObject jo = (JObject)JsonConvert.DeserializeObject(response);
                JArray ja = (JArray)jo["data"];
                jo = (JObject)ja[0];
                ja = (JArray)jo["rateByClientType"];
                jo = (JObject)ja[0];
                ja = (JArray)jo["ratesByType"];
                jo = (JObject)ja[0];
                jo = (JObject)jo["lastActualRate"];
                double sellRate = jo["sell"]["originalValue"].ToObject<double>();
                double buyRate = jo["buy"]["originalValue"].ToObject<double>();
                return (sellRate, buyRate);
            }
            catch (Exception e) { ex = e; };
            return (null, null);
        }
    }
}