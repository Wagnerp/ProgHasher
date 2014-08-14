/*
 * Created by SharpDevelop.
 * User: marteyj
 * Date: 1/9/2009
 * Time: 8:36 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using Logger;
using System.Collections;
using ProgHasher.Components;
using System.Collections.Generic;


namespace ProgHasher.Configuration
{
	/// <summary>
	/// Description of HasherConfig.
	/// </summary>
	public sealed class HasherConfig
	{
		public List PathList, DrivesList;
		public HasherList Locations;
		private IDictionary ENV;
        private ILog logger = null;
		
		private string HOMEDRIVE = string.Empty;
		private string WINDIR = string.Empty;
		private string SYSTEMROOT = string.Empty;
		private string PROGRAMFILES = string.Empty;
		private string STARTUP = string.Empty;
		private string DESKTOP = string.Empty;
		private string MUSIC = string.Empty;
		private string DOCUMENTS = string.Empty;
		private string PERSONAL = string.Empty;
		private string SYSTEM = string.Empty;
		private string PICTURES = string.Empty;
		private string DOTNET = "Microsoft.NET\\Framework\\";
		
		public static string PATH = "PATH";
		public static string programfiles = "PROGRAMFILES";
		public static string system = "SYSTEM";
		public static string others = "OTHERS";
		public static string drives = "DRIVES";
		public static string homedrive = "HOMEDRIVE";
		public static string windir = "WINDIR";
		
        /// <summary>
        /// Default ctor.
        /// </summary>
		public HasherConfig(ILog logger)
		{
            this.logger = logger;
            Locations = new HasherList();
			PathList = new List();
			DrivesList = new List();
			
			string[] path_locs = null;
			try{
				ENV = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
				
				try{
					string path = Environment.GetEnvironmentVariable(PATH, EnvironmentVariableTarget.Machine);
					path_locs = path.Split(';');
					foreach(string s in path_locs)
					{
						PathList.Add(s);
					}
				} catch {}
				
				PROGRAMFILES = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			  	SYSTEM = Environment.GetFolderPath(Environment.SpecialFolder.System);
			  	
			  	//get the logical drives.
			  	string[] logicaldrives = Environment.GetLogicalDrives();
			  	foreach(string s in logicaldrives)
			  	{
			  		//DrivesList.Add(s);
			  	}
			  	
				STARTUP = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
				DESKTOP = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				MUSIC = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
				DOCUMENTS = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				PERSONAL = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
				PICTURES = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
				
				Locations.SetList(others, MUSIC); Locations.SetList(others, DOCUMENTS);
				Locations.SetList(others, STARTUP); Locations.SetList(others, PICTURES);
				Locations.SetList(others, DESKTOP); Locations.SetList(others, PERSONAL);
												
				WINDIR = (string)ENV[windir.ToLower()];
				if(WINDIR == null)
					WINDIR = (string)ENV[windir];
				
				HOMEDRIVE = (string)ENV[homedrive];
				if(string.IsNullOrEmpty(HOMEDRIVE))
				{
					System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(WINDIR);
					HOMEDRIVE = di.Root.FullName;
				}
                if (!string.IsNullOrEmpty(WINDIR))
                {
                    if (logger != null)
                        logger.Warn("Windows directory not found!");
                    string frameworkLoc = System.IO.Path.Combine(WINDIR, DOTNET);
                    Locations.SetList(PATH, frameworkLoc);
                }
			}
			catch(System.Security.SecurityException e)
			{
				Console.WriteLine(e);
			}
			catch(System.Exception e)
			{
				Console.WriteLine(e);
			}
			//Locations.SetList(drives, DrivesList);
			Locations.SetList(homedrive, HOMEDRIVE);
			Locations.SetList(windir, WINDIR);
			Locations.SetList(programfiles, PROGRAMFILES);
			Locations.SetList(PATH, PathList);
			Locations.SetList(system, SYSTEM);
		}
		
		/// <summary>
		/// Get the list of directories a specified type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public List GetList(string type)
		{
			if(type != null)
				type = type.ToUpper();
			else
				return Locations[PATH];
			if(Locations.ContainsKey(type))
				return Locations[type];
			return new List();
		}
		
		/// <summary>
		/// Get the list of directories within the PATH variable.
		/// </summary>
		/// <returns></returns>
		public List<string> GetPathList()
		{
			return GetList(PATH);
		}
		
		/// <summary>
        /// Get the system directory location.
		/// </summary>
		/// <returns></returns>
		public List<string> GetSystemList()
		{
			return GetList(system);
		}
		
		/// <summary>
        /// Get a list of miscelleanous locationss.
		/// </summary>
		/// <returns></returns>
		public List<string> GetOthersList()
		{
			return GetList(others);
		}
		
        /// <summary>
        /// Get the Windows directory.
        /// </summary>
        /// <returns></returns>
		public List<string> GetWindirList()
		{
			return GetList(windir);
		}	
		
        /// <summary>
        /// Get the main drive of the current machine.
        /// </summary>
        /// <returns></returns>
		public List<string> GetHomeDriveList()
		{
			return GetList(homedrive);
		}	
		
        /// <summary>
        /// Get the Program files directory location.
        /// </summary>
        /// <returns>"Program files" location</returns>
		public List<string> GetProgramFilesList()
		{
			return GetList(programfiles);
		}
		
        /// <summary>
        /// Get the list of drives on the current machine
        /// </summary>
        /// <returns></returns>
		public List<string> GetDrivesList()
		{
			return GetList(drives);
		}
	}
}
