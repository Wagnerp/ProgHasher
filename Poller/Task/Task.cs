/*
 * Created by SharpDevelop.
 * User: marteyj
 * Date: 1/21/2009
 * Time: 3:37 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using ProgHasher.Brain;
using System.Threading;
using System.ComponentModel;
using ProgHasher.Components;
using ProgHasher.Brain.Engine;
using System.Collections.Generic;
using ProgHasher.Components.Events;

namespace ProgHasher.Poller.Tasks
{
	/// <summary>
	/// Allocates the task of searching a directory for wildcards.
	/// </summary>
	public class Task
	{			
		public event TaskCompletedHandler TaskCompleted;
		public event DirectoryPolledHandler DirectoryFound;
		public event FilePolledHandler FileAdded;
		
		public long Limit = 0;
		public int MaxDepth = 0;
		
		//private int THREAD_COUNT = 0;
		//private int THREAD_INDEX = 0;
		//private const int MAX_THREAD_COUNT = 4;
		
		private bool HALT = false;
		private int foundCount = 0;
		
		//private List<string> FoundList = null;
		//private ProcessList processList = null;
		
		public String location = string.Empty;
		public WildCardCollection names = null;
		internal ManualResetEvent DoneEvent = null;
		private TaskItem taskItem = null;
		
		//private List<ManualResetEvent> subEvents = null;
		internal Task(TaskItem item, WildCardCollection names, long limit, ManualResetEvent completed)
		{
			this.location = item.Name;
			this.taskItem = item;
			this.names = names;
			Limit = limit;
							
			//this.processList = new ProcessList();
			//this.processList.Add(item);
			//this.FoundList = new List<string>();
			//this.subEvents = new List<ManualResetEvent>();
			
			this.DoneEvent = completed;
		}
		
		public Task() {}
		
		internal bool IsSubDir = false;
		
		internal bool Taken = false;
		
		public long FoundCount { get { return foundCount; } }
		internal TaskItem Item {get { return taskItem; } }
		
		internal void BeginTask(Object threadContext)
		{
            Thread.CurrentThread.Name = string.Format("Task{0}", threadContext);
            //Console.WriteLine("i am doing something at {0}", (int)threadContext);
			//while(processList.NotProcessedCount > 0 && !HALT)
			{
				//while(this.HasSubTasks() && !HALT)
				{
					//if(THREAD_COUNT < MAX_THREAD_COUNT)
					{
					
						try{
							TaskItem subtask = TaskAvailable();
							//this.subEvents.Insert(this.THREAD_INDEX, new ManualResetEvent(false));
							//Console.WriteLine("Starting searchengine to find {0} in '{1}'", this.names.Value, subtask.Name);
							//HashEngine search = new HashEngine(subtask, name, this.subEvents[(int)this.ProcessedCount-1]);
							HashEngine search = new HashEngine(subtask, names);
							
							//map events.
							search.SearchCompleted += new TaskCompletedHandler(SearchCompleted);
							search.DirectoryPolled += new DirectoryPolledHandler(DirectorySeen);
							search.FilePolled += new FilePolledHandler(FilePolled);
							
							
							/*if(ThreadPool.QueueUserWorkItem(search.BeginSearch, THREAD_INDEX))
							{
								++THREAD_COUNT;
								++THREAD_INDEX;
							}*/
							search.BeginSearch();
						}
						catch(System.Exception e)
						{
							Console.WriteLine(e);
						}
					}
					//wait to ensure to avoid race conditions.
					//Thread.Sleep(2000);
				}
				
			}
			//if(subEvents != null && subEvents.Count > 0)
			//	WaitHandle.WaitAll(subEvents.ToArray());
				
			SetComplete((int)threadContext);
		}
		
		/*
		internal bool HasSubTasks()
		{
			lock(this.processList)
			{
				if(processList.NotProcessedCount > 0)
				{
					Console.WriteLine("we have more: {0} left", processList.NotProcessedCount);
					return true;
				}
				
				return false;
			}
		}
		*/
		
		internal TaskItem TaskAvailable()
		{
			return this.taskItem;
		}
		
		private void SetComplete(int threadIndex)
		{
			DoneEvent.Set();
			OnTaskCompleted(new HashEventArgs(threadIndex));
		}
		
		private void OnTaskCompleted(HashEventArgs e)
		{
			if(TaskCompleted != null)
				TaskCompleted(this, e);
		}
		
		private void SearchCompleted(object o, HashEventArgs e)
		{
			//THREAD_COUNT--;
			//THREAD_INDEX = e.TaskID;
			//Console.WriteLine("i finished a search");	
		}
		
		private void DirectorySeen(object sender, HashEventArgs e)
		{
			try
			{
				if(DirectoryFound != null)
				{
					DirectoryFound(new TaskItem(e.Task.ToLower(), e.TaskID), new HashEventArgs(e.Task, e.TaskID));
				}
			}
			catch(System.Exception ex)
			{
				Console.WriteLine(ex);
			}
		}
		
		
		private void FilePolled(object sender, HashEventArgs e)
		{
			if(!HALT)
			{
				foundCount++;
				if(FileAdded != null)
				{
					FileAdded(new object(), new HashEventArgs(e.Task));
				}
			}
			if(this.FoundCount >= this.Limit)
			{
				HALT = true;
			}
		}
	}
	
	/// <summary>
	/// Task Item class; stores name and location.
	/// </summary>
	public class TaskItem
	{
		private string name;
		public int Depth = 0;
		public int MaxDepth = 0;
		public TaskItem(string name, int depth)
		{
			this.name = name;
			this.Depth = depth;
		}
			
		
		public TaskItem(string name)
		{
			this.name = name;
		}
		
		
		public string Name
		{
			get{ return name; }
			set{ name = value; }
		}
	}
	
	/// <summary>
	/// Stores a list of tasks.
	/// </summary>
	public class ProcessList : List<Task>
	{		
		public bool Contains(string name)
		{
			foreach(Task item in this)
			{
				if(item.location.CompareTo(name.ToLower()) == 0)
				   return true;
			}
			return false;
		}
	}
}
