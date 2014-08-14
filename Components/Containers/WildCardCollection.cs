/*
 * Created by SharpDevelop.
 * User: MarteyJ
 * Date: 1/14/2009
 * Time: 11:33 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;

namespace ProgHasher.Components
{
	/// <summary>
	/// Description of WildCardCollection.
	/// </summary>
	public class WildCardCollection : System.Collections.Generic.List<string>
	{
		public WildCardType Type = WildCardType.Regular;
		
		public WildCardCollection()
		{
		}
		
		public WildCardCollection(params string[] items)
		{
			foreach(string item in items)
			{
				Add(item);
			}
		}
		
		public WildCardCollection(string items)
		{
			string[] values = items.Split(';',',');
			foreach(string item in values)
			{
				Add(item);
			}
		}
		
		public WildCardCollection(WildCardType type, params string[] items)
		{
			foreach(string item in items)
			{
				Add(item);
			}
			this.Type = type;
		}
		
		public new void Add(string item)
		{
			item = item.Trim();
			if(!this.Contains(item))
			{
				base.Add(item);
			}
			base[base.IndexOf(item)] = item;
		}
		
		public String Value
		{
			get{
				System.Text.StringBuilder result = new System.Text.StringBuilder();
				
				int count = 0;
				foreach(string s in this)
				{
					count++;
					result.Append(s);
					if(count < this.Count)
						result.Append(";");
				}
				return result.ToString();
			}
		}

        public bool Contains(WildCardCollection collection)
        {
            bool success = false;
            if (collection != null)
            {
                foreach (string s in collection)
                {
                    if (this.Contains(s))
                    {
                        success = true;
                        break;
                    }
                }
            }
            return success;
        }
	}
	

	public enum WildCardType : int
	{
		Extension = 0,
		Extreme = 1,
		Regular = 2,
		Exact = 3,
	}
}
