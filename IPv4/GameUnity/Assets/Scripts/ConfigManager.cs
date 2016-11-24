using System;
using System.IO;
using UnityEngine;

public interface IConfigManager
{
	string GetInformationFromFile(string fullPath, string defaultValue);
}

public class ConfigManager : IConfigManager
{
	public string GetInformationFromFile(string fullPath, string defaultValue)
	{
		if (RuntimePlatform.Android == Application.platform)
		{
			string fileName = Path.GetFileName(fullPath);
			using (MemoryStream m = new MemoryStream())
			{
				string obbfile = Application.dataPath;
				Ionic.Zip.ZipFile zipfile = new Ionic.Zip.ZipFile(obbfile);
				Ionic.Zip.ZipEntry ze = zipfile["assets/" + fileName];
				if (ze != null)
				{
					ze.Extract(m);
					m.Seek(0, SeekOrigin.Begin);
					using (StreamReader s = new StreamReader(m))
					{
						string fileText = s.ReadLine();
						return fileText;
					}
				}
			}
		}
		else
		{
			if (File.Exists(fullPath))
			{
				String fileText = File.ReadAllText(fullPath);
				return fileText;
			}
		}

		return defaultValue;
	}
}