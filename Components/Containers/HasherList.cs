/*
 * Created by SharpDevelop.
 * User: marteyj
 * Date: 1/22/2009
 * Time: 2:49 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using ProgHasher.Poller.Tasks;
using System.Collections.Generic;


namespace ProgHasher.Components
{
	/// <summary>
	/// A container used to store items in keys and values.
	/// </summary>
	public class HasherList :
		System.Collections.Generic.Dictionary<String, List>
	{
		public long Size;
		
        /// <summary>
        /// Insert a list at a certain
        /// </summary>
        /// <param name="key">key of item</param>
        /// <param name="value">list to store</param>
		internal void SetList(string key, List value)
		{
			if(key != string.Empty && value != null)
			{
                if (this.ContainsKey(key))
                {
                    List tmp = this[key];
                    tmp.Add(value);
                    this[key] = tmp;
                }
                else
                {
                    this.Add(key, value);
                }
			}
		}
		
        /// <summary>
        /// Insert a value at a certain key in the list.
        /// </summary>
        /// <param name="key">key of item</param>
        /// <param name="value">item to store</param>
		internal void SetList(string key, string value)
		{
			if(key != string.Empty)
			{
				if(this.ContainsKey(key))
				{
					if(!this[key].Contains(value))
					{
						this[key].Add(value);
						Size++;
					}
				}
				else
				{
					this.Add(key, new List());
					this.SetList(key, value);
				}
			}	
		}
		
		/// <summary>
		/// Set the extension of a list stored at a given key.
		/// </summary>
		/// <param name="key">key of item</param>
		/// <param name="extensions">extensions to store</param>
		internal void SetExt(string key, string[] extensions)
		{
			if(key != string.Empty && key != null && extensions != null)
			{
				if(this.ContainsKey(key))
				{
					this[key].Extensions = extensions;
				}
				else
				{
					this.Add(key, new List());
					this.SetExt(key, extensions);
				}
			}	
		}
		
        /// <summary>
        /// Get the list associated with a wildcard (or name).
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
		internal List Get(string name)
		{
			Dictionary<string, List> tmpList = new Dictionary<string, List>(this);
			foreach(KeyValuePair<String, List> kvp in tmpList)
			{
				if(kvp.Value.ContainsExt(name))
				{
					return kvp.Value;
				}
			}
			if(this.ContainsKey("PATH"))
				return this["PATH"];
			return new List();
		}
		
		/// <summary>
		/// Get the list associated with a set of wildcards.
		/// </summary>
		/// <param name="extensions"></param>
		/// <returns></returns>
		internal List Get(WildCardCollection extensions)
		{
			List result = null;
			if(extensions != null && extensions.Count > 0)
			{
				result = new List();
				foreach(string ext in extensions)
					result.Add(Get(ext));
				return result;
			}
			return Get("PATH");
		}
		
        /// <summary>
        /// concatenate another list with this one.
        /// </summary>
        /// <param name="list"></param>
		public void ConcatenateAll(HasherList list)
		{
			if(list == null)
				return;
			foreach(KeyValuePair<String, List> kvp in list)
			{
				this.SetList(kvp.Key, kvp.Value);
			}
		}
		
        /// <summary>
        /// Write the key value pairs onto console.
        /// </summary>
		public void WriteValues()
		{
			Dictionary<string, List> tmpList = new Dictionary<string, List>(this);
			foreach(KeyValuePair<String, List> kvp in tmpList)
			{
				Console.WriteLine("{0}:-> {1}",kvp.Key, kvp.Value.Value);
			}
		}
	}
	
		/// <summary>
		/// Class to store search locations by type and extension.
		/// </summary>
		public class List : System.Collections.Generic.List<string>
		{
			//public List<string> Extensions = null;
			public String[] Extensions = null;
			
            /// <summary>
            /// Add a list unto this list.
            /// </summary>
            /// <param name="list"></param>
			public void Add(List list)
			{
				this.AddRange(list);
			}
			
            /// <summary>
            /// check if an item contains the given extension
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
			public bool ContainsExt(string value)
			{
				if(Extensions == null)
				{
					return false;
				}
				foreach(string s in Extensions)
				{
					if(s.ToLower().CompareTo(value.ToLower()) == 0)
					{
						return true;
					}
				}
				return false;
			}
			
            /// <summary>
            /// Return the semi-colon delimited values of the list.
            /// </summary>
			public String Value
			{
				get{
					string value = string.Empty;
				
					foreach(string s in this)
					{	
						if(value.Length > 1)
							value = string.Format("{0};{1}", value, s);
						else
							value += s;
					}
					return value;
				}
			}
		}
		
		
		/// <summary>
		/// List of TaskItem
		/// </summary>
		public class RuleList : Dictionary<string, TaskItemList>
		{
			internal long Size;
			
            /// <summary>
            /// Add an item to the list
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
			internal void SetList(string key, TaskItem value)
			{
				if(key != string.Empty)
				{
					if(this.ContainsKey(key))
					{
						if(!this[key].Contains(value.Name))
						{
							this[key].Add(value);
							Size++;
						}
					}
					else
					{
						this.Add(key, new TaskItemList());
						this.SetList(key, value);
					}
				}	
			}
			
            /// <summary>
            /// Add a list of items to container
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
			internal void SetList(string key, TaskItemList value)
			{
				if(key != string.Empty && value != null)
				{
					foreach(TaskItem s in value)
					{
						this.SetList(key, s);
					}
				}
			}
		}
		
        /// <summary>
        /// List of items.
        /// </summary>
		public class TaskItemList : List<TaskItem>
		{
			public bool Contains(string key)
			{
				foreach(TaskItem item in this)
				{
					if(item.Name.CompareTo(key) == 0 )
						return true;
				}
				return false;
			}
		}

}
