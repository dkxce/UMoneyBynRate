//////////////////////////////////////
// dkxce Sberbank BYN Rate Grabber ///
//////////////////////////////////////

using System.Net;
using System.Text;

namespace UMoneyBynRate
{
    public class SberMoneyGrabber : RateGrabber, IRateGrabber
    {
        private List<string> AllowedTransfersCategories = new List<string>(new string[] { "CUTransfersPro", "CUTransfersPrivate" });

        public SberMoneyGrabber()
        {
            name = "Sberbank BYN Rate Grabber";
            //url = "http://www.sberbank.ru/proxy/services/rates/public/actual?rateType=ERNP-2&isoCodes[]=BYN";
            url = "https://mainfin.ru/bank/sberbank/currency/byn?name=&from=1001";
        }

        public SberMoneyGrabber(string url) : base(url)
        {
            name = "Sberbank BYN Rate Grabber";
        }

        public override (double?, double?) GetRates(out Exception ex)
        {
            ex = null;
            HttpWebRequest wreq = (HttpWebRequest)HttpWebRequest.Create(url);
            wreq.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/111.0)";
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
                int bb = response.IndexOf("buy_byn");
                if (bb < 0) throw new Exception("Exchange rate not found");
                int sb = response.IndexOf("sell_byn");
                if (sb < 0) throw new Exception("Exchange rate not found");

                string buy_text = response.Substring(bb);
                buy_text = buy_text.Substring(buy_text.IndexOf(">") + 1);
                buy_text = buy_text.Substring(0, buy_text.IndexOf("<"));
                
                string sell_text = response.Substring(sb);
                sell_text = sell_text.Substring(sell_text.IndexOf(">") + 1);
                sell_text = sell_text.Substring(0, sell_text.IndexOf("<"));

                double sellRate = double.Parse(sell_text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
                double buyRate = double.Parse(buy_text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
                return (sellRate, buyRate);
            }
            catch (Exception e) { ex = e; };
            return (null, null);
        }
    }
}