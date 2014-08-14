/*
 * Created by SharpDevelop.
 * User: marteyj
 * Date: 1/9/2009
 * Time: 3:40 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Threading;
using ProgHasher.Components;
using ProgHasher.Components.Timer;

namespace ProgHasher.Components.Events
{
	/// <summary>
	/// Description of HashEvent.
	/// </summary>
	public class HashEventArgs : EventArgs 
	{
		public readonly string Task;
		public readonly int TaskID;
		public readonly WildCardCollection WildCards;
		public readonly object List;
		
		public HashEventArgs(string task)
		{
			this.Task = task;
		}
		
		public HashEventArgs(string task, int taskId)
		{
			this.Task = task;
			this.TaskID = taskId;
		}
		
		public HashEventArgs(WildCardCollection wildcards)
		{
			this.WildCards = wildcards;
		}
		
		public HashEventArgs(int taskID)
		{
			this.TaskID = taskID;
		}
		
		public HashEventArgs(object list)
		{
			this.List = list;
		}
	}
	
	
	public class HashItemSearchEventArgs : EventArgs
	{
		public readonly WildCardCollection WildCards;
		public readonly String Name;
		
		public HashItemSearchEventArgs(WildCardCollection wildcard, String name)
		{
			this.WildCards = wildcard;
			this.Name = name;
		}
	}
	
	public class TimerEventArgs : EventArgs
	{
		public TimerType Type;
		public TimerEventArgs(TimerType timertype)
		{
			this.Type = timertype;
		}
	}
	
	public delegate void HashTimerEventHandler(object o, TimerEventArgs e);
	public delegate void HashItemSearchHandler(object o, HashItemSearchEventArgs e);
	public delegate void HashCompletedHandler(object o, HashEventArgs e);
	public delegate void TaskCompletedHandler(object o, HashEventArgs e);
	public delegate void HashPollCompletedHandler(object o, HashEventArgs e);
	public delegate void DirectoryPolledHandler(object o, HashEventArgs e);
	public delegate void FilePolledHandler(object o, HashEventArgs e);
	public delegate void HashItemFoundHandler(object o, HashEventArgs e);
	
	
	public class ManualResetEvents : System.Collections.Generic.List<ManualResetEvent>
	{
		private int max = 64, index = 0;
		/*public ManualResetEvent[] ToArray()
		{
			ManualResetEvent[] array = new ManualResetEvent[this.Count];
			for(int i = 0; i < this.Count; ++i)
			{
				array[i] = this[i];
			}
			return array;
		}
		*/
		
		public new void Add(ManualResetEvent item)
		{
			lock(this)
			{
				if(base.Count < max )
					base.Add(item);
				else {
					index = (index >= max) ? 0 : index;
					base[index] = item;
					index++;
				}
			}
		}
	}
}
