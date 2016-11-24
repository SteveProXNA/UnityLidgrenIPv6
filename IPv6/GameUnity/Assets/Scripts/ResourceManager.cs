using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

public interface IResourceManager
{
	Object LoadResourceImmediate(Type resourceType, string fileNameWithPath);
	String GetInformationFromFile(String text, String key);
}

public class ResourceManager : IResourceManager
{
	public Object LoadResourceImmediate(Type resourceType, string fileNameWithPath)
	{
		string path = String.Empty;
		string filename = fileNameWithPath;
		string ext = String.Empty;

		if (fileNameWithPath.LastIndexOf("/") > -1)
		{
			filename = fileNameWithPath.Substring(fileNameWithPath.LastIndexOf("/") + 1);
			path = fileNameWithPath.Substring(0, fileNameWithPath.LastIndexOf("/"));

		}
		if (filename.LastIndexOf(".") > -1)
		{
			ext = filename.Substring(filename.LastIndexOf(".") + 1);
			filename = filename.Substring(0, filename.LastIndexOf("."));
		}

		string load = String.Format("{0}/{1}", path, filename);
		return Resources.Load(load, resourceType);
	}

	public String GetInformationFromFile(String text, String data)
	{
		using (StringReader file = new StringReader(text))
		{
			while (true)
			{
				string lineStr = file.ReadLine();
				if (lineStr == null)
					break;

				string[] array = lineStr.Split(new[] { ',' });
				string key = array[0];
				string value = array[1];

				if (key == data)
				{
					return value;
				}
			}
		}

		return String.Empty;
	}
}