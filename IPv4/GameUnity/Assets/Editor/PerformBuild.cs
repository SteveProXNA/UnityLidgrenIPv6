using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

class PerformBuild
{
	private const string IoSdefaultBundleId = "com.steveproxna.gameunity";
	private const string AndroidDefaultBundleId = "com.steveproxna.gameunity";

	[MenuItem("Automated/Automated Android Build")]
	private static void CommandLineBuildOnCheckinAndroid()
	{
		const BuildTarget target = BuildTarget.Android;

		// Get build scenes.
		string[] levels = GetBuildScenes();
		string locationPathName = CommandLineReader.GetCustomArgument("APKPath");
		const BuildOptions options = BuildOptions.None;


		// Android specific command line arguments parsed first.
		string versionCode = CommandLineReader.GetCustomArgument("VersionCode");
		int verCode;
		if (Int32.TryParse(versionCode, out verCode))
		{
			PlayerSettings.Android.bundleVersionCode = verCode;
		}

		// Get command line arguments (if passed in to build job).
		CommandLineData commandLineData = GetCommandLineData(AndroidDefaultBundleId);

		string homeDirectory = CommandLineReader.GetCustomArgument("HomeDirectory");
		if (!homeDirectory.EndsWith("Assets"))
		{
			homeDirectory += "\\Assets";
		}

		//ensure deployment script is up to date with correct version code and bundle info.
		string apkName = Path.GetFileNameWithoutExtension(locationPathName);
		//WriteVersionCodeAndAPKNameToScript(commandLineData.Environment, verCode, apkName, homeDirectory);

		if (commandLineData.StreamingAssetsEnabled == "true")
		{
			BuildPipelineBuildAssetBundle(BuildTarget.Android);
			DeleteMasterAssetResources();
		}


		// Build all scenes.
		BuildPipelineBuildPlayer(levels, locationPathName, target, options, commandLineData);
	}

	[MenuItem("Automated/Automated iOS Build")]
	static void CommandLineBuildOnCheckinIOS()
	{
		const BuildTarget target = BuildTarget.iPhone;

		// Get build scenes.
		string[] levels = GetBuildScenes();
		const string locationPathName = "iOSbuild";
		const BuildOptions options = BuildOptions.None;

		// Get command line arguments (if passed in to build job).
		CommandLineData commandLineData = GetCommandLineData(IoSdefaultBundleId);


		PlayerSettings.iOS.scriptCallOptimization = ScriptCallOptimizationLevel.FastButNoExceptions;

		var shortBundleVersion = CommandLineReader.GetCustomArgument("ShortBundleVersion");
		if (shortBundleVersion == String.Empty)
			shortBundleVersion = commandLineData.BuildVersion;

		PlayerSettings.shortBundleVersion = shortBundleVersion;


		if (commandLineData.StreamingAssetsEnabled == "true")
		{
			BuildPipelineBuildAssetBundle(BuildTarget.iPhone);
			DeleteMasterAssetResources();
		}

		// Build all scenes.
		BuildPipelineBuildPlayer(levels, locationPathName, target, options, commandLineData);
	}

	private static string[] GetBuildScenes()
	{
		List<string> names = new List<string>();
		foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
		{
			if (e == null)
			{
				continue;
			}

			if (e.enabled)
			{
				names.Add(e.path);
			}
		}
		return names.ToArray();
	}
	
	private static CommandLineData GetCommandLineData(string defaultBundleId)
	{
		string environment = CommandLineReader.GetCustomArgument("GameServer");
		string svnrevision = CommandLineReader.GetCustomArgument("SvnRevision");
		string version = CommandLineReader.GetCustomArgument("Version");

		// Opportunity to override the default bundle ID.
		string overrideBundleId = CommandLineReader.GetCustomArgument("BundleID");
		string streamingAssetsEnabled = CommandLineReader.GetCustomArgument("StreamingAssetsEnabled");
		string bundleId = (0 == overrideBundleId.Length) ? defaultBundleId : overrideBundleId;

		return new CommandLineData(environment, svnrevision, version, bundleId, streamingAssetsEnabled);
	}
	private static void WriteVersionCodeAndAPKNameToScript(string environment, int versionCode, string apkName, string scriptPath)
	{
		scriptPath = scriptPath + "\\build";
		string[] scriptLines = File.ReadAllLines(scriptPath);

		//replace the lines we want to replace.
		for (int i = 0; i < scriptLines.Length; i++)
		{
			if (scriptLines[i].Contains("set versioncode="))
				scriptLines[i] = "set versioncode=" + versionCode;

			if (scriptLines[i].Contains("set apkname="))
				scriptLines[i] = "set apkname=" + apkName;
		}

		File.WriteAllLines(scriptPath, scriptLines);
	}
	private static void SetIcon(BuildTarget target, string environment)
	{
		//destination where the icon we pick should end up
		string finalIconResourcePath = "Assets/Resources/Textures/GameIcon/GameIcon.png";

		string platformIconFolder = "";
		if (target == BuildTarget.Android)
			platformIconFolder = "Android";
		if (target == BuildTarget.iPhone)
			platformIconFolder = "IOS";
		if (target == BuildTarget.StandaloneWindows)
			platformIconFolder = "Windows";

		const string fileName = "GameIcon.png";
		string finalIconPath = "Assets/Resources/Textures/GameIcon/" + platformIconFolder + "/" + fileName;

		//copy platform specific icon to destination icon path, and refresh asset db.
		FileUtil.CopyFileOrDirectory(finalIconPath, finalIconResourcePath);
		AssetDatabase.Refresh();
	}

	private static void BuildPipelineBuildAssetBundle(BuildTarget buildTarget)
	{
		string[] assetPaths = AssetDatabase.GetAllAssetPaths();
		BuildAssetBundlesCommon(buildTarget, assetPaths, Application.streamingAssetsPath);
	}
	private static void DeleteMasterAssetResources()
	{
		string path = Application.dataPath + "/Resources/Master Assets/";
		Directory.Delete(path, true);
	}
	private static void BuildAssetBundlesCommon(BuildTarget buildTarget, string[] assetPaths, string outputPath)
	{
		Debug.Log(assetPaths.Length + " assets found");

		string pathName = outputPath;
		int assetsBuilt = 0;
		foreach (string f in assetPaths)
		{

			Object a = Resources.LoadAssetAtPath(f, typeof(Object));
			if (a == null)
			{
				continue;
			}

			Object[] asset = new Object[1];
			asset[0] = a;

			string assetType = a.GetType().Name;
			if (assetType.Equals("Object"))
			{
				continue;
			}

			// No need to process materials.
			if (assetType.StartsWith("Material"))
			{
				continue;
			}

			string assetName = assetType + "_" + asset[0].name + ".unity3d";
			string fullName = pathName + "/" + assetName;

			const BuildAssetBundleOptions options = BuildAssetBundleOptions.CollectDependencies |
													BuildAssetBundleOptions.CompleteAssets |
													BuildAssetBundleOptions.UncompressedAssetBundle;

			BuildPipeline.BuildAssetBundle(a, asset, fullName, options, buildTarget);
			assetsBuilt++;

		}
		Debug.Log("Streaming asset build complete. Assets built: " + assetsBuilt);
	}
	private static void BuildPipelineBuildPlayer(string[] levels, string locationPathName, BuildTarget target, BuildOptions options, CommandLineData commandLineData)
	{
		string environment = commandLineData.Environment;
		string svnrevision = commandLineData.SvnRevision;
		string version = commandLineData.BuildVersion;
		string bundleId = commandLineData.BundleIdent;
		string streaminAssetsEnabled = commandLineData.StreamingAssetsEnabled;
		if (levels == null || levels.Length == 0 || locationPathName == null)
		{
			return;
		}

		Debug.Log(String.Format("Path: \"{0}\"", locationPathName));
		for (int i = 0; i < levels.Length; ++i)
		{
			Debug.Log(String.Format("Scene[{0}]: \"{1}\"", i, levels[i]));
		}

		string fileRoot = Application.streamingAssetsPath;

		// Check environment settings.
		string enviromentText = "PlayerSettings Environment=\"" + environment + "\"";
		if (0 == environment.Length)
		{
			environment = "127.0.0.1";
			enviromentText += " " + environment;
		}

		// Environment specific.
		if (0 != environment.Length)
		{
			// Persist environment in local file for client to load at runtime.
			string fullPath = fileRoot + "/GameServer.txt";
			File.WriteAllText(fullPath, environment);
		}

		// SvnRevision specific.
		string svnRevisionText = "PlayerSettings SVNrevision=\"" + svnrevision + "\"";
		if (0 == svnrevision.Length)
		{
			svnrevision = "0";
			svnRevisionText += " [client will default to \"" + "\"0]";
		}

		if (0 != svnrevision.Length)
		{
			// Persist svnrevision in local file for client to load at runtime.
			string fullPath = fileRoot + "/SvnRevision.txt";
			File.WriteAllText(fullPath, svnrevision);
		}

		if (0 != version.Length)
		{
			// Persist build version in local file for client to load at runtime.
			string fullPath = fileRoot + "/BuildVersion.txt";
			File.WriteAllText(fullPath, version);
		}

		if (0 != streaminAssetsEnabled.Length)
		{
			// Persist build version in local file for client to load at runtime.
			string fullPath = fileRoot + "/StreamingAssetsEnabled.txt";
			File.WriteAllText(fullPath, streaminAssetsEnabled);
		}

		PlayerSettings.bundleIdentifier = bundleId;
		string bundleVersion = (0 == version.Length) ? "1.0" : version;
		PlayerSettings.bundleVersion = bundleVersion;
		//SetIcon(target, environment);



		Debug.Log(enviromentText);
		Debug.Log(svnRevisionText);
		Debug.Log("Streaming Assets Enabled: " + streaminAssetsEnabled);
		Debug.Log("PlayerSettings Version=\"" + bundleVersion + "\"");
		Debug.Log("PlayerSettings BundleID=\"" + bundleId + "\"");

		Debug.Log("Starting Build!");
		String error = BuildPipeline.BuildPlayer(levels, locationPathName, target, options);
		if (!String.IsNullOrEmpty(error))
		{
			throw new System.Exception("Build failed: " + error);
		}
		Debug.Log("Complete Build!");
	}
}

// http://forum.unity3d.com/threads/unityengine-ui-dll-is-in-timestamps-but-is-not-known-in-assetdatabase.274492/
class ReimportUnityEngineUI
{
	[MenuItem("Automated/Reimport UI Assemblies", false, 100)]
	public static void ReimportUI()
	{
		var path = EditorApplication.applicationContentsPath + "/UnityExtensions/Unity/GUISystem/{0}/{1}";
		var version = Regex.Match(Application.unityVersion, @"^[0-9]+\.[0-9]+\.[0-9]+").Value;

		string engineDll = String.Format(path, version, "UnityEngine.UI.dll");
		string editorDll = String.Format(path, version, "Editor/UnityEditor.UI.dll");

		ReimportDll(engineDll);
		ReimportDll(editorDll);
	}

	private static void ReimportDll(string path)
	{
		if (File.Exists(path))
			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
		else
			Debug.LogError(String.Format("DLL not found {0}", path));
	}
}

struct CommandLineData
{
	public string Environment { get; private set; }
	public string SvnRevision { get; private set; }
	public string BuildVersion { get; private set; }
	public string BundleIdent { get; private set; }
	public string StreamingAssetsEnabled { get; private set; }

	public CommandLineData(string environment, string svnrevision, string version, string bundleId, string streamingAssetsEnabled)
		: this()
	{
		Environment = environment;
		SvnRevision = svnrevision;
		BuildVersion = version;
		BundleIdent = bundleId;
		StreamingAssetsEnabled = streamingAssetsEnabled;
	}
}