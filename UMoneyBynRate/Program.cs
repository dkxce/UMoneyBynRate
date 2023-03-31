///////////////////////////////////
//      dkxce Rate Grabber       //
///////////////////////////////////

using UMoneyBynRate;

internal class Program
{
    private static int Main(string[] args)
    {
        RateGrabber.AddGrabber(new UMoneyBYNRateGrabber());
        RateGrabber.AddGrabber(new TinkoffMoneyGrabber());
        RateGrabber.AddGrabber(new AlfabankMoneyGrabber());
        RateGrabber.Start();
        return 0;
    }
}