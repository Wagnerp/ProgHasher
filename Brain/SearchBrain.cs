/*
 * Created by SharpDevelop.
 * User: marteyj
 * Date: 1/27/2009
 * Time: 12:33 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;
using System.Xml;
using ProgHasher.Brain;
using ProgHasher.Components;
using ProgHasher.Poller.Tasks;
using ProgHasher.Configuration;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Logger;

namespace ProgHasher.Brain
{
	/// <summary>
	/// The brains behind the correct selection of folders to make searches faster.
	/// </summary>
	public class SearchBrain
	{
		private static WildCardCollection WildCards = null;
		public SearchRules Rules = null;
        private ILog logger;
		internal static SearchList List = new SearchList();
		
		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="wildcards">this represents search keywords. eg:"autoexec.bat"</param>
		private SearchBrain(WildCardCollection wildcards)
		{
			GetExtensions(wildcards);
			Rules = new SearchRules();
			ParseList();
		}

        private SearchBrain(WildCardCollection wildcards, ILog logger)
        {
            GetExtensions(wildcards);
            Rules = new SearchRules();
            this.logger = logger;
            ParseList();
        }
		
		public SearchBrain()
		{
			Rules = new SearchRules();
			ParseList();
		}

        public SearchBrain(ILog logger)
        {
            Rules = new SearchRules();
            this.logger = logger;
            ParseList();
        }
		
		public static WildCardCollection GetExtensions(WildCardCollection wildcards)
		{
			WildCards = new WildCardCollection();
			if(wildcards != null)
			{
				foreach(string s in wildcards)
				{
					try{
						FileInfo fi = new FileInfo(s);
						WildCards.Add(fi.Extension.Remove(0,1));
					}
					catch
					{
						string[] name = s.Split('.');
						WildCards.Add(name[name.Length-1]);
					}
				}
			}
			return WildCards;
		}
		
		
		public Rule[] GetRule(WildCardCollection wildcards)
		{
			GetExtensions(wildcards);
			return Rules.GetRule(WildCards);
		}
		
		/// <summary>
        /// Parse a configuration file with the following structure into rules for hashing:
		/// </summary>
        /// <example>
        ///    &lt;Configuration&gt;
        ///		  	&lt;Programs&gt;
        ///		  		&lt;ProgHasher&gt;
        ///		  			&lt;SearchList&gt;
        ///		  				&lt;Music extensions=""&gt;
        ///		                   &lt;HashFirst&gt;
        ///		  						&lt;Location&gt;...............&lt;/Location&gt;
        ///		                   &lt;/HashFirst&gt;
        ///		  					&lt;HashAfter&gt;	
        ///		  						&lt;Location&gt;...............&lt;/Location&gt;
        ///		  					&lt;/HashAfter&gt;
        ///		  				&lt;/Music&gt;
        ///		  				&lt;Executable extensions=""&gt;
        ///		  					&lt;HashFirst&gt;
        ///		  						&lt;Location&gt;...............&lt;/Location&gt;
        ///		                   &lt;/HashFirst&gt;
        ///		  					&lt;HashAfter&gt;	
        ///		  						&lt;Location&gt;...............&lt;/Location&gt;
        ///		  					&lt;/HashAfter&gt;
        ///		  				&lt;/Executable&gt;
        ///		  			&lt;/SearchList&gt;
        ///		  		&lt;/ProgHasher&gt;
        ///		  	&lt;/Programs&gt;
        ///		 &lt;/Configuration&gt;
        /// </example>
		private void ParseList()
		{
			try
			{				
				XmlDocument xDoc = new XmlDocument();
				xDoc.Load(ConfigurationManager.ConfigLocation);
				XmlNodeList searchlist = xDoc.SelectNodes("Configuration/Programs/ProgHasher/SearchList/*");
				XmlNodeList excludeList = xDoc.SelectNodes("Configuration/Programs/ProgHasher/ExclusionList/*");
				
				//Create exclusion list for the poller.
				List<Regex> exclusionList = new List<Regex>();
				foreach(XmlNode x in excludeList)
				{
					Regex r = new Regex(x.InnerText, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);
					exclusionList.Add(r);
				}
				SearchRules.ExclusionList.AddRange(exclusionList);
				
                //Start parsing the stuff.
				string[] hashes = new string[]{ "HashFirst", "HashAfter"};
				foreach(XmlNode x in searchlist)
				{
					Rule newRule = null;
					string extensions = x.Attributes["extensions"].Value;
					WildCardCollection wildcards = null;
					if(extensions != null)
						wildcards = new WildCardCollection(extensions);
					newRule = new Rule(wildcards, x.Name);
					foreach(string s in hashes)
					{
						string xPathString = string.Format("Configuration/Programs/ProgHasher/SearchList/{0}/{1}//Location", x.Name, s);
						XmlNodeList locations = xDoc.SelectNodes(xPathString);
						if(locations != null){
							foreach(XmlNode x2 in locations)
							{
								int depth = 0;
								int.TryParse(x2.Attributes["depth"].Value, out depth);
								newRule.SetHash(s, x2.InnerText, depth);
							}
						}
					}
                    if(logger != null)
					    logger.Info(string.Format("Adding newly created rule for: {0} of type: {1}", wildcards.Value, x.Name));
					Rules.Add(wildcards, newRule);
				}
			}
			catch(System.Exception e)
			{
                if (logger != null)
                    logger.Error(e);
			}
		}


        public bool HasOldValidCache(string name, string type)
        {
            string cacheLoc = ConfigurationManager.OutputLocation;
            XmlDocument xDoc = new XmlDocument();
            DateTime dt = new DateTime();
            try
            {
                xDoc.Load(cacheLoc);
                string xPathString = string.Format("Storage/ProgHasher//{0}[@name = '{0}' and @type = '{1}']", name, type);
                XmlNodeList nodes = xDoc.SelectNodes(xPathString);
                string date = nodes.Item(0).Attributes["datecached"].Value;
                dt = DateTime.Parse(date);
                if (!(dt.Ticks.CompareTo(DateTime.Now.AddDays(-7).Ticks) < 0))
                {
                    if (logger != null)
                        logger.Info(string.Format("Last cached date: {0}. Keeping records." + dt.ToShortDateString()));
                    return true;
                }
            }
            catch (System.Exception e)
            {
                if (logger != null)
                    logger.Error(e);
            }
            if (logger != null)
                logger.Info(string.Format("Last cached date: {0}. Ignoring records." + dt.ToShortDateString()));
            return false;
        }
	}
	
	/// <summary>
	/// Keeps track of search rules populated from config.
	/// </summary>
	public class SearchRules : Dictionary<WildCardCollection, Rule> 
	{
		public static System.Collections.Generic.List<Regex> ExclusionList = new List<Regex>();
		
        /// <summary>
        /// Gets the rule associated with a set of wildcards.
        /// </summary>
        /// <param name="wildcards">wildcard to find rule for</param>
        /// <returns>list of rules containing search criteria for wildcard</returns>
		public Rule[] GetRule(WildCardCollection wildcards)
		{
			List<Rule> rules = new List<Rule>();
			WildCardCollection w = SearchBrain.GetExtensions(wildcards);
			foreach(string wildcard in w)
			{
				List<Rule> rule = this.FindRule(wildcard);
				if(rule.Count > 0)
				{
					rules.AddRange(rule);
				}
				else
				{
					rules.AddRange(this.GetGenericRule());
				}
			}
			
			return rules.ToArray();
		}
		
		/// <summary>
		/// find a rule based on a wildcard value
		/// </summary>
		/// <param name="key">semi-colon delimited wildcards</param>
		/// <returns>a list of rules found</returns>
		internal List<Rule> FindRule(string key)
		{
            string[] keys = key.Split(';');
            List<Rule> rules = new List<Rule>();
            foreach(KeyValuePair<WildCardCollection, Rule> kvp in this)
			{
				foreach(String s in kvp.Key)
				{
                    foreach (string k in keys)
                    {
                        if (k.ToLower().CompareTo(s) == 0)
                            rules.Add(kvp.Value);
                    }
				}
			}
            return rules;
		}
		
		/// <summary>
		/// gets the generic rules
		/// </summary>
		/// <returns></returns>
		internal List<Rule> GetGenericRule()
		{
			return this.FindRule("exe;doc");
		}
	}
	
	
	/// <summary>
    /// Rule is made up of a locations and its associated hashfirst and hashafter values.
	/// </summary>
	public class Rule : RuleList
	{
		public WildCardCollection wildcards;
		public String Name;
		
		/// <summary>
		/// ctor
		/// </summary>
		/// <param name="wildcards">wildcards</param>
		/// <param name="name"></param>
		public Rule(WildCardCollection wildcards, string name)
		{
			this.wildcards = wildcards;
			this.Name = name;
		}
		
		/// <summary>
		/// sets the value of the hash table to the created rule.
		/// </summary>
		/// <param name="name">name of rule</param>
		/// <param name="location">the location associated with the rule</param>
		/// <param name="maxdepth">the config value for the max depth to search</param>
		public void SetHash(string name, string location, int maxdepth)
		{
			List locations = SearchBrain.List.Config.GetList(location);
			foreach(string loc in locations)
			{
				TaskItem item = new TaskItem(loc);
				item.MaxDepth = maxdepth;
				this.SetList(name.ToUpper(), item);
			}
		}
	}
}
