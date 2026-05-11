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

using System;
using System.Diagnostics;

using UnityEditor;
using UnityEngine;

using Debug = UnityEngine.Debug;

[HelpURL("https://makaka.org/unity-assets")]
[InitializeOnLoad]
public class PublisherWelcomeScreen:EditorWindow
{
	private static PublisherWelcomeScreen window;
	private static Vector2 windowsSize = new Vector2(500f, 300f);
	private Vector2 scrollPosition;

	private static string windowHeaderText = "Unity Asset Store Publisher";
	private string copyright = "© Copyright " + DateTime.Now.Year + " Makaka Games";
	
	private const string isShowAtStartEditorPrefs = "WelcomeScreenShowAtStart";
	private static bool isShowAtStart = true;

	private static bool isInited;

	private static GUIStyle headerStyle;
	private static GUIStyle copyrightStyle;

	private static Texture2D allOurAssetsIcon;
	private static Texture2D docsIcon;
	private static Texture2D youTubeIcon;
	private static Texture2D unityConnectIcon;
	private static Texture2D vkIcon;
	private static Texture2D facebookIcon;
	private static Texture2D supportIcon;
	private static Texture2D instagramIcon;
	private static Texture2D twitterIcon;

	static PublisherWelcomeScreen()
	{
		EditorApplication.update -= GetShowAtStart;
		EditorApplication.update += GetShowAtStart;
	}

	private void OnGUI()
	{
		if (!isInited) 
		{
			Init();
		}
		
		if (GUI.Button(new Rect(0f, 0f, windowsSize.x, 58f), "", headerStyle))
		{
			Process.Start("https://makaka.org");
		}

		GUILayoutUtility.GetRect(position.width, 70f);

		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

		if (DrawButton(docsIcon, "Online Documentation — Latest", "You also have offline docs in Unity Package.")) 
			Process.Start("https://makaka.org/unity-assets");

		if (DrawButton(supportIcon, "Support — info@makaka.org", "First of all, read the docs. If it didn't help, get the support.")) 
			Process.Start("mailto:info@makaka.org?subject=Makaka Games - Asset Support");

		if (DrawButton(allOurAssetsIcon, "Unity Assets", "All AR assets. Create your own Pokemon GO.")) 
			Process.Start("https://makaka.org/o/all-unity-assets");

		if (DrawButton(youTubeIcon, "YouTube Channel", "Unity Tutorials & Demos.")) 
			Process.Start("https://www.youtube.com/makakaorg");
		
		if (DrawButton(facebookIcon, "Facebook", "News page.")) 
			Process.Start("https://www.facebook.com/makakaorg");

		if (DrawButton(twitterIcon, "Twitter", "News page.")) 
			Process.Start("https://www.twitter.com/makakaorg/");
		
		if (DrawButton(unityConnectIcon, "Unity Connect", "Asset Store Publishers.")) 
			Process.Start("https://connect.unity.com/g/asset-store-publishers");

		if (DrawButton(instagramIcon, "Instagram", "News page.")) 
			Process.Start("https://www.instagram.com/makakaorg/");

		if (DrawButton(vkIcon, "VK", "News page.")) 
			Process.Start("https://vk.com/makakaorg");
		
		EditorGUILayout.EndScrollView();

		EditorGUILayout.LabelField(copyright, copyrightStyle);
	}

	private static bool Init()
	{
		try
		{
			headerStyle = new GUIStyle();
			headerStyle.normal.background = Resources.Load("HeaderLogo") as Texture2D;
			headerStyle.normal.textColor = Color.white;
			headerStyle.fontStyle = FontStyle.Bold;
			headerStyle.padding = new RectOffset(340, 0, 27, 0);
			headerStyle.margin = new RectOffset(0, 0, 0, 0);

			copyrightStyle = new GUIStyle();
			copyrightStyle.alignment = TextAnchor.MiddleRight;

			docsIcon = Resources.Load("Docs") as Texture2D;
			allOurAssetsIcon = Resources.Load("AR") as Texture2D;
			supportIcon = Resources.Load("Support") as Texture2D;
			youTubeIcon = Resources.Load("YouTube") as Texture2D;
			unityConnectIcon = Resources.Load("UnityConnect") as Texture2D;
			vkIcon = Resources.Load("VK") as Texture2D;
			facebookIcon = Resources.Load("Facebook") as Texture2D;
			instagramIcon = Resources.Load("Instagram") as Texture2D;
			twitterIcon = Resources.Load("Twitter") as Texture2D;
			
			isInited = true; 
		}
		catch (Exception e)
		{
			Debug.Log("WELCOME SCREEN INIT: " + e);
			return false;
		}

		return true;
	}

	private static bool DrawButton(Texture2D icon, string title = "", string description = "")
	{
		GUILayout.BeginHorizontal();

		GUILayout.Space(34f);
		GUILayout.Box(icon, GUIStyle.none, GUILayout.MaxWidth(48f), GUILayout.MaxHeight(48f));
		GUILayout.Space(10f);

		GUILayout.BeginVertical();

		GUILayout.Space(1f);
		GUILayout.Label(title, EditorStyles.boldLabel);
		GUILayout.Label(description);

		GUILayout.EndVertical();

		GUILayout.EndHorizontal();

		Rect rect = GUILayoutUtility.GetLastRect();
		EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

		GUILayout.Space(10f);

		return Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition);
	}


	private static void GetShowAtStart()
	{
		EditorApplication.update -= GetShowAtStart;
		
		isShowAtStart = EditorPrefs.GetBool(isShowAtStartEditorPrefs, true);

		if (isShowAtStart)
		{
			EditorApplication.update -= OpenAtStartup;
			EditorApplication.update += OpenAtStartup;
		}
	}

	private static void OpenAtStartup()
	{
		if (isInited && Init()) 
		{
			OpenWindow();

			EditorApplication.update -= OpenAtStartup;
		}
	}

	[MenuItem("Tools/Makaka Games/Welcome Screen", false)]
	public static void OpenWindow()
	{
		if (window == null) 
		{
			window = GetWindow<PublisherWelcomeScreen> (true, windowHeaderText, true);
			window.maxSize = window.minSize = windowsSize;
		}
	}

	private void OnEnable()
	{
		window = this;
	}

	private void OnDestroy()
	{
		window = null;

		EditorPrefs.SetBool(isShowAtStartEditorPrefs, false);
	}
}