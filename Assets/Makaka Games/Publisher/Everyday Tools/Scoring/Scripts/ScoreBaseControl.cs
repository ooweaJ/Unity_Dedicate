/*
===================================================================
Unity Assets by MAKAKA GAMES: https://makaka.org/o/all-unity-assets
===================================================================

Online Docs (Latest): https://makaka.org/unity-assets
Offline Docs: You have a PDF file in the package folder.

=======
SUPPORT
=======

First of all, read the docs. If it didn’t help, get the support.

Web: https://makaka.org/support
Email: info@makaka.org

If you find a bug or you can’t use the asset as you need, 
please first send email to info@makaka.org (in English or in Russian) 
before leaving a review to the asset store.

I am here to help you and to improve my products for the best.
*/

public class ScoreBaseControl
{
    public static string thousandLiteral = "K";
    public static string millionLiteral = "M";

    public static float Truncate(float value, int digits)
    {
        double mult = System.Math.Pow(10.0d, digits);
        double result = System.Math.Truncate(mult * value ) / mult;
        return (float) result;
    }

    public static string Round (float value, int digits)
    {   
        if (value >= 1000000)
        {
            return Truncate((value / 1000000f), digits).ToString() + millionLiteral;
        }
        else
        {
            return value.ToString();
        }
    }
}
