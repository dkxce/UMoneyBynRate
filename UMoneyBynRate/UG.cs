﻿///////////////////////////////////
// dkxce UMoney BYN Rate Grabber //
///////////////////////////////////

using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UMoneyBynRate
{    
    public class UMoneyBYNRateGrabber: RateGrabber, IRateGrabber
    {
        public UMoneyBYNRateGrabber() 
        {
            name = "UMoney BYN Rate Grabber";
            url = "https://yoomoney.ru/account/exchange-rates?lang=ru"; 
        }

        public UMoneyBYNRateGrabber(string url): base(url)
        {
            name = "UMoney BYN Rate Grabber";            
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
                int iof = response.IndexOf("{\"currencyCode\":\"BYN\"");
                if (iof < 0) throw new Exception("Exchange rate not found");
                int lof = response.IndexOf("}", iof);
                string data = response.Substring(iof, lof - iof + 1);

                JObject jo = (JObject)JsonConvert.DeserializeObject(data);
                double sellRate = jo["sellRate"].ToObject<double>();
                double buyRate = jo["buyRate"].ToObject<double>();
                return (sellRate, buyRate);
            }
            catch (Exception e) { ex = e; };
            return (null, null);
        }
    }
}
