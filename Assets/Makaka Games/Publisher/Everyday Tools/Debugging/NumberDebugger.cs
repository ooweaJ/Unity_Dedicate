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

using UnityEngine;

[HelpURL("https://makaka.org/unity-assets")]
public class NumberDebugger
{
	private float valuePreviousForDebugFloatAbsChanging;
	private int counterForDebugFloatAbsChanging;

    public void DebugFloatAbsChanging(float delta, float valueCurrent)
    {
		valueCurrent = Mathf.Abs(valueCurrent);

        if (Mathf.Abs(valuePreviousForDebugFloatAbsChanging - valueCurrent) > delta)
        {
            Debug.Log(
				counterForDebugFloatAbsChanging++ + ". "
				+ "New = " + valueCurrent 
				+ ", Old = " + valuePreviousForDebugFloatAbsChanging);
        }

		valuePreviousForDebugFloatAbsChanging = valueCurrent;
    }
}
