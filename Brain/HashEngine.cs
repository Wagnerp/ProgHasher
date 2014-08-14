/*
 * Created by SharpDevelop.
 * User: MarteyJ
 * Date: 1/12/2009
 * Time: 10:26 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;
using System.Threading;
using ProgHasher.Components;
using ProgHasher.Poller.Tasks;
using System.Collections.Generic;
using ProgHasher.Components.Events;


namespace ProgHasher.Brain.Engine
{
	/// <summary>
	/// Description of HashEngine.
	/// </summary>
	public class HashEngine
	{
		private String location = null;
		private WildCardCollection names = null;
		//private ManualResetEvent completed = null;
		private TaskItem task = null;
		
		public event TaskCompletedHandler SearchCompleted;
		public event DirectoryPolledHandler DirectoryPolled;
		public event FilePolledHandler FilePolled;
		
		private System.Collections.Generic.List<string> SubDirectories = null;
		private System.Collections.Generic.List<string> Files = null;
		
		//public HashEngine(string location, WildCardCollection names, ManualResetEvent completed)
		public HashEngine(string location, WildCardCollection names)
		{
			//this.completed = completed;
			this.task = new TaskItem(location);
			this.location = task.Name;
			this.names = names;
			//this.IgnoreSubDirs = ignoreSubDirs;
			Files = new System.Collections.Generic.List<string>();
			SubDirectories = new System.Collections.Generic.List<string>();
		}
		
		public HashEngine(TaskItem location, WildCardCollection names)
		{
			this.task = location;
			this.location = task.Name;
			this.names = names;
			Files = new System.Collections.Generic.List<string>();
			SubDirectories = new System.Collections.Generic.List<string>();
		}
		
		public void BeginSearch()
		//public void BeginSearch(Object threadContext)
		{
			GetItems();
			//this.SetCompleted(threadContext);
			this.SetCompleted();
		}
		
		private void GetItems()
		{
			try{
				DirectoryInfo dirInfo = null;
				if(Directory.Exists(location))
					dirInfo = new DirectoryInfo(location);
				if(dirInfo != null)
				{
					DirectoryInfo[] subdirs = dirInfo.GetDirectories();
					foreach(DirectoryInfo d in subdirs)
					{
						SubDirectories.Add(d.FullName);
						OnDirectoryPolled(new HashEventArgs(d.FullName, task.Depth+1));
					}
					
					foreach(string wildcard in names)
					{
						FileInfo[] files = null;
						if(names.Type == WildCardType.Regular)
							files = dirInfo.GetFiles(wildcard+"*");
						else if(names.Type == WildCardType.Extension)
							files = dirInfo.GetFiles("*."+wildcard);
						else if(names.Type == WildCardType.Exact)
							files = dirInfo.GetFiles(wildcard);
						else
							files = dirInfo.GetFiles("*"+wildcard+"*");
						if(files != null)
						{
							foreach(FileInfo f in files)
							{
								Files.Add(f.FullName);
								OnFilePolled(new HashEventArgs(f.FullName));
							}
						}
					}
				}
			}
			catch(System.Exception e)
			{
				System.Console.WriteLine(e);
				throw e;
			}
		}
		
		private void SetCompleted()
		//private void SetCompleted(Object threadContext)
		{
			//completed.Set();
			OnSearchCompleted(new HashEventArgs(location));
		}
		
		private void OnSearchCompleted(HashEventArgs e)
		{
			if(SearchCompleted != null)
					SearchCompleted(this, e);
		}
		
		private void OnDirectoryPolled(HashEventArgs e)
		{
			if(DirectoryPolled != null)
				DirectoryPolled(this, e);
		}
		
		private void OnFilePolled(HashEventArgs e)
		{
			if(FilePolled != null)
				FilePolled(this, e);
		}
		
	}
}
