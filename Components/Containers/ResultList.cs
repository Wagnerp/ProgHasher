/*
 * Created by SharpDevelop.
 * User: MarteyJ
 * Date: 1/13/2009
 * Time: 4:29 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;
using System.Collections.Generic;
using ProgHasher.Components.Events;

namespace ProgHasher.Components
{
	/// <summary>
	/// Description of ResultList.
	/// </summary>
	public class ResultList : HasherList
	{
		public event HashItemSearchHandler ItemNotFound;
		private bool hasChanged = false;
		
		public bool HasChanged
		{
			get {
				if(hasChanged){
					hasChanged = false;
					return true;
				}
				return hasChanged;
			}
            internal set
            {
                hasChanged = value;
            }
		}
		
		public String Status = string.Empty;
				
		public void Convert(System.Collections.Generic.List<string> result)
		{
			if(result != null)
			{
				foreach(string s in result)
				{
					try
					{
						FileInfo f = new FileInfo(s);
						SetList(f.Name.ToLower(), f.FullName.ToLower());
					}
					catch(System.Exception e)
					{
						Console.WriteLine("The file disappeared for some reason. \n{0}", e);
					}
				}
			}
		}
		
		public void Add(ResultList list)
		{
			if(list != null)
			{
				foreach(string s in list.Keys)
				{
					this.SetList(s, list[s]);
				}
			}
			hasChanged = true;
		}
		
		public void Convert(string result)
		{
			if(result != null)
			{
				try
				{
					FileInfo f = new FileInfo(result);
					SetList(f.Name.ToLower(), f.FullName.ToLower());
				}
				catch(System.Exception e)
				{
					Console.WriteLine("The file disappeared for some reason. \n{0}", e);
				}
			}
		}
		
		public bool Contains(string key, string wildcard)
		{
			key = key.Trim();
			wildcard = wildcard.Trim();
			if(key.ToLower().EndsWith(wildcard.ToLower()))
			   return this.ContainsKey(key.ToLower());
						
			return (this.ContainsKey(key+"."+wildcard));
		}
		
		
		public bool Contains(string key, WildCardCollection wildcards)
		{
			lock(this)
			{
                if (wildcards != null)
                {
                    foreach (string wildcard in wildcards)
                    {
                        if (Contains(key, wildcard))
                            return true;
                    }
                    if (ItemNotFound != null)
                    {
                        ItemNotFound(this, new HashItemSearchEventArgs(wildcards, key));
                    }
                }
                else
                    return base.ContainsKey(key);
				return false;
			}
		}
		
		private string FindKeyPath(string key)
		{
			if(this.ContainsKey(key))
			{
				return this[key][0];
			}
			return null;
		}
		
		
		public KeyValuePair<string, List> Find(string item)
		{
			return new KeyValuePair<string, List>();
		}
		
		
		public void WriteList()
		{
			lock(this)
			{
				Dictionary<string, List> tmpList = new Dictionary<string, List>(this);
				foreach(KeyValuePair<string, List> kvp in tmpList)
				{
					foreach(string s in kvp.Value)
					{
						Console.WriteLine("{0}:-> {1}",kvp.Key, s);
					}
				}
				Console.WriteLine();
			}
		}
	}
}
