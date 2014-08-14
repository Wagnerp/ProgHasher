/*
 * Created by SharpDevelop.
 * User: marteyj
 * Date: 1/8/2009
 * Time: 9:29 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using Logger;
using ProgHasher.Components;
using System.Collections.Generic;

using ProgHasher.Components.Events;
using System.Threading;

namespace ProgHasher
{
	class Program
	{
		//private static ResultList results = new ResultList();
		private static WildCardCollection wildcard = null;
		private static Hasher hasher;
        private static ILog logger = LogManager.GetLogger(typeof(ProgHasher.Program));

        /// <summary>
        /// Main Entry point
        /// </summary>
        /// <param name="args">arguments</param>
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");
            Thread.CurrentThread.Name = "main";
			if(args.Length > 0)
				hasher = new Hasher(args[0], ProgHasher.HashSize.Infinity, logger);
			else
				hasher = new Hasher(Console.ReadLine(), ProgHasher.HashSize.Infinity, logger);
			
			try
            {
				hasher.ItemFound += new HashItemFoundHandler(OnItemFound);
				hasher.HashCompleted += new HashCompletedHandler(SetResults);				
				hasher.Wildcard.Type = WildCardType.Extension;
				hasher.BeginHash();
				wildcard = hasher.Wildcard;
				ExecuteRunner();
                //ExecuteSearcher();
				hasher.EndHash();
                //hasher.EndSearch();
			}
			catch(Exception e)
			{
				logger.Error(e.Message, e);
			}
			
			Console.Write("Press any key to exit . . . ");
			Console.ReadKey(true);
		}

        /// <summary>
        /// Searcher: Tests the searching part of the Hasher.
        /// </summary>
        private static void ExecuteSearcher()
        {
            while (true)
            {
                //results = hasher.GetResults();
                Console.Write(">>> ");
                string line = Console.ReadLine();
                string[] vals = line.Split(' ');
                line = vals[0];
                if (line.CompareTo("exit") == 0)
                    break;
                if (line.CompareTo("stop") == 0)
                    hasher.EndSearch();
                else if (line.CompareTo("list") == 0)
                    hasher.Results.WriteList();
                else if (line.CompareTo("size") == 0)
                    Console.WriteLine("{0} :: {1}", hasher.Results.Count, hasher.Results.Size);
                else if (line.CompareTo("nullify") == 0)
                    hasher.Results.Clear();
                else if (line.CompareTo("status") == 0)
                    Console.WriteLine(hasher.Results.Status);
                else if(vals.Length > 1)
                {
                    if (line.ToLower().CompareTo("find") == 0)
                    {
                        if (hasher.Results.Contains(vals[1].ToLower(), wildcard))
                        {
                            Console.WriteLine("Command exists!");
                        }
                        else
                        {
                            Console.WriteLine("Command not found!");
                        }
                    }
                    else if (line.ToLower().CompareTo("search") == 0)
                    {
                        hasher.BeginSearch(vals[1], WildCardType.Extreme);
                    }
                    else
                        Console.WriteLine("wtf are u saying");
                }
            }
            Console.WriteLine("exited");
            hasher.EndSearch();
        }
		
        /// <summary>
        /// Sets the results when invoked.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
		public static void SetResults(object o, HashEventArgs e)
		{
			/*lock(results){
				results = (ResultList)o;
				wildcard = e.WildCards;
				
			}*/
			if(wildcard != null)
                Console.WriteLine("setting results! Size: {0} and wildcard: {1}", hasher.Results.Size, wildcard.Value);
            else
                Console.WriteLine("setting results! Size: {0} and wildcard: null", hasher.Results.Size);
		}
		
		/// <summary>
		/// When an item if found.
		/// </summary>
		/// <param name="foundItem"></param>
		/// <param name="e"></param>
		public static void OnItemFound(object foundItem, HashEventArgs e)
		{
			//lock(results){results = (ResultList)foundItem;}
            //Console.WriteLine("found something");
            //System.Threading.Thread.Sleep(2000);
		}
		
        /// <summary>
        /// Runner: Tests the hashing capabilities of the Hasher.
        /// </summary>
		public static void ExecuteRunner()
		{
			while(true)
			{
				//results = hasher.GetResults();
				Console.Write(">>> ");
				string line = Console.ReadLine();
				if(line.CompareTo("exit") == 0 )
					break;
                if (line.CompareTo("stop") == 0)
                    hasher.AbortAll();
                else if (line.CompareTo("list") == 0)
                    hasher.Results.WriteList();
                else if (line.CompareTo("size") == 0)
                    Console.WriteLine("{0} :: {1}", hasher.Results.Count, hasher.Results.Size);
                else if (line.CompareTo("nullify") == 0)
                    hasher.Results.Clear();
                else if (line.CompareTo("status") == 0)
                    Console.WriteLine(hasher.Results.Status);
                else if (line.CompareTo("end") == 0)
                    break;
                else if (hasher.Results.Contains(line.ToLower(), wildcard))
                {
                    Console.WriteLine("command exists!");
                }
                else
                {
                    Console.WriteLine("Command not found!");
                }
			}
			Console.WriteLine("exited");
			hasher.AbortAll();
		}
	}
}
