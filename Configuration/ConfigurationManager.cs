/*
 * Created by SharpDevelop.
 * User: marteyj
 * Date: 1/8/2009
 * Time: 9:51 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using Logger;
using System.IO;
using System.Reflection;

namespace ProgHasher.Configuration
{
	/// <summary>
	/// Description of ConfigurationManager.
	/// </summary>
	public static class ConfigurationManager
	{
		private static string fileName = "configuration.xml";
		private static string fileStreamName = "execache.xml";
		private static string configlocation = string.Empty;
		public static string OutputLocation = string.Empty;
        private static ILog logger = LogManager.GetLogger(typeof(ConfigurationManager));

		static ConfigurationManager()
		{
			configlocation = Path.Combine(System.Environment.CurrentDirectory, "Settings\\" + fileName);
            OutputLocation = Path.Combine(System.Environment.CurrentDirectory, "Settings\\" + fileStreamName);
			LoadConfigurations();
		}
		
		public static FileStream CreateOutputStream(params string[] location)
		{
			string filename = string.Empty;
			System.IO.FileInfo fi = null;
			System.IO.FileStream fs = null;
			if(location.Length > 0)
			{
				try
				{
					fi = new FileInfo(location[0]);
					OutputLocation = fi.FullName;
					fs = new FileStream(fi.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
				}
				catch(System.IO.IOException e)
				{
					if(logger != null)
                        logger.Error(e);
				}
			}
			else
			{
				try{
					fs = new FileStream(OutputLocation, FileMode.OpenOrCreate, FileAccess.ReadWrite);
				}
				catch(System.IO.IOException e)
				{
                    if(logger != null)
                        logger.Error(e);
				}
			}
			return fs;
		}
		
		private static void LoadConfigurations()
		{
			VerifyLocation();
			if(configlocation != null)
			{
				ConfigLocation = configlocation;
			}
			else
				ConfigLocation = null;
		}
		
		private static void VerifyLocation()
		{
			if(!File.Exists(configlocation))
				configlocation = null;
		}
		
		public static String ConfigLocation = string.Empty;
	}
}
