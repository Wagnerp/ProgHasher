/*
 * Created by SharpDevelop.
 * User: marteyj
 * Date: 1/8/2009
 * Time: 9:31 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using Logger;
using System.Threading;
using ProgHasher.Brain;
using ProgHasher.Poller;
using System.ComponentModel;
using ProgHasher.Components;
using ProgHasher.Poller.Tasks;
using System.Collections.Generic;
using ProgHasher.Components.Timer;
using ProgHasher.Components.Events;

namespace ProgHasher
{	
	/// <summary>
	/// Size enumerator
	/// </summary>
	public enum HashSize : int
	{
		Infinity = int.MaxValue,
		Medium = 500,
		Large = 5000,
	}
	
    /// <summary>
    /// Hash modes.
    /// Search -> for searches.
    /// Hash -> for hashes, allows priority searches when items are not in list.
    /// </summary>
	internal enum HashMode
	{
		Hash = 0,
		Search,
	}
	
	/// <summary>
	/// Description of Hasher.
	/// </summary>
	public class HasherBase
	{
        internal ILog logger = null;
		
        internal TaskPoller poller = null;
		internal TaskPoller priorityPoller = null;
				
		internal SearchBrain brain = null;
        private Int64 StartTime = 0L;
        private bool PerformCacheAnalysis = false;
		
		public long Size = 0;
		public WildCardCollection Wildcard = null;
		public static long MaxPerFolder = 0;
		
		protected BackgroundWorker runner, priorityRnr = null;
        protected ProgTimer hashTimer = null;
				
		private ResultList results, Cache = null;
		private HashMode mode;
		private bool priorityWorking = false;
		
        /// <summary>
        /// Event: Occurs when the hash or search is completed.
        /// </summary>
		public event HashCompletedHandler HashCompleted;

        /// <summary>
        /// Event: Occurs when an Item is found, whether hashing or searching.
        /// </summary>
		public event HashItemFoundHandler ItemFound;

        public HasherBase(ILog logger) : this()
        {
            this.logger = logger;
        }
		
        /// <summary>
        /// Initialize class components
        /// </summary>
		protected HasherBase()
		{
            //runner
            this.runner = new BackgroundWorker();
            this.runner.WorkerSupportsCancellation = true;
            this.runner.DoWork += new DoWorkEventHandler(this.BGWInitTasks);
            this.runner.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BWGRunWorkerCompleted);

            //priorityRnr
            this.priorityRnr = new BackgroundWorker();
            this.priorityRnr.WorkerSupportsCancellation = true;
            this.priorityRnr.DoWork += new DoWorkEventHandler(this.BeginPriorityTasks);
            this.priorityRnr.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PriorityTaskCompleted);

            //hashTimer
            this.hashTimer = new ProgTimer(TimerType.ResultHash, TimerType.WriteOutput);

            //init. the search engine.
            this.brain = new SearchBrain();
            
            //initialize other stuff
            MaxPerFolder = SearchBrain.List.MaxPerFolder;
			Thread.CurrentThread.IsBackground = true;
			Thread.CurrentThread.Priority = ThreadPriority.Highest;
			results = new ResultList();
			Cache = new ResultList();
            hashTimer.TimerFired += new HashTimerEventHandler(this.OnTimerFired);
            results.ItemNotFound += new HashItemSearchHandler(this.OnItemNotFound);
		}

        /// <summary>
        /// Results of the search.
        /// </summary>
        public ResultList Results
        {
            get
            {
                return results;
            }
        }

		/// <summary>
		/// Start hashing based on the given requirements
		/// </summary>
		public void BeginHash()
		{
			mode = HashMode.Hash;

            if (brain.HasOldValidCache("FoundList", Wildcard.Value))
            {
                this.results = SearchBrain.List.LoadPriorCachedList("FoundList", Wildcard.Value);
                this.results.ItemNotFound += new HashItemSearchHandler(OnItemNotFound);
                this.PerformCacheAnalysis = true;
                this.Results.HasChanged = true;
            }
			runner.RunWorkerAsync(this);
			hashTimer.BeginTimer();
		}
		
		
        /// <summary>
        /// Start a search routine.
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="type"></param>
		public void BeginSearch(string keyword, WildCardType type)
		{
			mode = HashMode.Search;
            bool isactive = false;
            WildCardCollection w = new WildCardCollection(type, keyword);
            if (priorityRnr != null && priorityPoller != null)
            {
                isactive = priorityPoller.IsActive(w);
                if (priorityRnr.IsBusy && !isactive)
                {
                    priorityRnr.CancelAsync();
                }
            }
            if (!isactive)
            {
                this.priorityPoller = new TaskPoller(w, brain.Rules.GetRule(w), logger);
                this.priorityPoller.HashItemFound += new HashItemFoundHandler(OnPriorityItemFound);
                this.priorityPoller.HashCompleted += new HashCompletedHandler(OnHashCompleted);
                this.priorityPoller.IgnoreDepthRestriction = true;
                this.priorityRnr.RunWorkerAsync();
                this.hashTimer.BeginTimer();
            }
		}
		

		/// <summary>
		/// Hold everyone still till entire hash is completed.
		/// </summary>
		public void EndHash()
		{
			for(int i = 0; i < 5; i++){
				if(runner.IsBusy || priorityRnr.IsBusy)
				{
					Thread.Sleep(2000);
				}
			}
			if(runner.IsBusy)
				runner.CancelAsync();
			if(priorityRnr.IsBusy)
				priorityRnr.CancelAsync();
		}

        /// <summary>
        /// Try to end searching. Hold everything still till search aborts.
        /// </summary>
        public void EndSearch()
        {
            this.EndHash();
        }

		
		/// <summary>
		/// Abort all hash tasks and stay idle.
		/// </summary>
		public void AbortAll()
		{
			try{
				//if(!ar.IsCompleted)
				if(runner != null && !runner.CancellationPending && runner.IsBusy)
				{
					runner.CancelAsync();
				}
				if(hashTimer != null && hashTimer.IsBusy && !hashTimer.StopPending)
					hashTimer.StopTimer();
                if (priorityRnr != null && priorityRnr.IsBusy && !priorityRnr.CancellationPending)
                    priorityRnr.CancelAsync();
			}
			catch(System.Exception e)
			{
				Console.WriteLine(e);
			}
		}
		

		/// <summary>
		/// Find the size required.
		/// </summary>
		/// <param name="size">size indicator</param>
		protected void GetSize(HashSize size)
		{
			switch(size)
			{
                case HashSize.Infinity: this.Size = (int)HashSize.Infinity; break;
                case HashSize.Large: this.Size = (int)HashSize.Large; break;
                case HashSize.Medium: this.Size = (int)HashSize.Medium; break;
			}
		}
		
        /// <summary>
        /// Validate search requirements
        /// </summary>
		protected void ValidateRequest()
		{
			try{
				SearchBrain.List.MaxPerFolder = (int)Size;
				if(Size > MaxPerFolder)
				{
					if(logger != null)
                        logger.Warn("Warning: Requiring a hash size greater than allowed");
				}
				poller = new TaskPoller(this.Wildcard, brain.GetRule(this.Wildcard), logger);
				poller.HashPollCompleted += new HashPollCompletedHandler(this.OnPollCompleted);
				poller.HashItemFound += new HashItemFoundHandler(this.OnItemFound);
				poller.HashCompleted += new HashCompletedHandler(this.OnHashCompleted);
			}
			catch(System.Exception e)
			{
				if(logger != null)
                    logger.Error(e);
			}
		}
		
		/// <summary>
		/// List of inspected directories by the normal task poller.
		/// </summary>
		public System.Collections.Generic.List<string> InspectedDirectories
		{
			get{ return poller.CompletedDirs; }
            private set { poller.CompletedDirs = value; }
		}
	
		/// <summary>
		/// EventHandler: Begins the hashing task.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void BGWInitTasks(object sender, DoWorkEventArgs e)
		{
            Thread.CurrentThread.Name = "runner";
            if(sender != null)
			{
                if (this.PerformCacheAnalysis)
                {
                    this.InspectedDirectories = this.Results["completeddirs"];
                    this.Results.Remove("completeddirs");
                }
                BackgroundWorker bw = sender as BackgroundWorker;
                this.StartTime = hashTimer.TimeElapsed;
	            poller.InitRequest(bw);
	            if (bw.CancellationPending)
	            {
	                e.Cancel = true;
	            }
            }
		}
		
		/// <summary>
		/// Starts Priority tasks.
		/// </summary>
		/// <param name="sender">sender</param>
		/// <param name="e">e</param>
		protected void BeginPriorityTasks(object sender, DoWorkEventArgs e)
		{
            Thread.CurrentThread.Name = "priority";
            BackgroundWorker bw = sender as BackgroundWorker;
            this.priorityPoller.InitRequest(bw);
            this.StartTime = hashTimer.TimeElapsed;
            if(mode == HashMode.Hash)
                hashTimer.AddTimer(TimerType.PriorityTimeout);
            if (bw.CancellationPending)
            {
                e.Cancel = true;
            }
		}
		
		/// <summary>
		/// EventHandler: Occurs when a priority task is completed.
		/// </summary>
		/// <param name="sender">sender</param>
		/// <param name="e">e</param>
		protected void PriorityTaskCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Cancelled)
            {
                // The user canceled the operation.
                if(logger != null)
                    logger.Debug("Operation was canceled");
            }
            else if (e.Error != null)
            {
                // There was an error during the operation.
                string msg = String.Format("An error occurred: {0}", e.Error);
                if(logger != null)
                    logger.Error(msg);
            }
            else
            {
                // The operation completed normally.
                string msg = String.Format("Result = {0}", e.Result);
                if(logger != null)
                    logger.Warn(msg);
            }
            if(mode == HashMode.Hash)
                hashTimer.RemoveTimer(TimerType.PriorityTimeout);
		}
		
		
		/// <summary>
		/// When the runner stops. Verify the cause
		/// </summary>
		/// <param name="sender">sender</param>
		/// <param name="e">e</param>
        protected void BWGRunWorkerCompleted(object sender, 
            RunWorkerCompletedEventArgs e)
        {   
			if (e.Cancelled)
            {
                // The user canceled the operation.
                if (logger != null)
                    logger.Debug("Operation was canceled");
                if(hashTimer.IsBusy)
            		hashTimer.StopTimer();
            }
            else if (e.Error != null)
            {
                // There was an error during the operation.
                string msg = String.Format("An error occurred: {0}", e.Error);
                if (logger != null)
                    logger.Error(msg);
                if(hashTimer.IsBusy)
            		hashTimer.StopTimer();
            }
            else
            {
                // The operation completed normally.
                string msg = String.Format("Result = {0}", e.Result);
                if (logger != null)
                    logger.Info(msg);
            }
        }
		
		
		/// <summary>
		/// Get the results of a hash or search.
		/// </summary>
		/// <returns>container of results.</returns>
		public ResultList GetResults()
		{
			return Results;
		}
		
		/// <summary>
		/// Event: occurs when all directories request have
        /// been polled successfully and are awaiting hash or search.
		/// </summary>
		/// <param name="o">o</param>
		/// <param name="e">e</param>
		protected void OnPollCompleted(object o, HashEventArgs e)
		{
            if (logger != null)
                logger.Info("I am done polling all folders.");
		}
		
        /// <summary>
        /// EventHandler: occurs when all hash is completed.
        /// </summary>
        /// <param name="o">o</param>
        /// <param name="e">e</param>
		protected void OnHashCompleted(object o, HashEventArgs e)
		{
			Console.WriteLine("All folders completely hashed");
			//Results.Convert(poller.Results); //MORE TIME CONSUMING!
			if(HashCompleted != null)
				HashCompleted(this.Results, e);
            if (logger != null)
                logger.Info(string.Format("Hashing completed in {0} ms", hashTimer.TimeElapsed - StartTime));
		}
		
		/// <summary>
		/// EventHandler: occurs when an item following the search 
		/// </summary>
		/// <param name="o"></param>
		/// <param name="e"></param>
		protected void OnItemFound(object o, HashEventArgs e)
		{
			//Console.WriteLine("Found: {0}", e.Task);
			lock(Cache)
			{				
				Cache.Convert(e.Task);
				Cache.Status = poller.Status;
			}
		}
		
		/// <summary>
		/// Event: occurs when an item is found using the priority poller.
        /// Of course...only Hasher cares about this...no other user should.
		/// </summary>
		/// <param name="o">o</param>
		/// <param name="e">e</param>
		protected void OnPriorityItemFound(object o, HashEventArgs e)
		{
            if (logger != null)
                logger.Info(string.Format("Found: {0}", e.Task));
			lock(Cache)
			{				
				Cache.Convert(e.Task);
				Cache.Status = priorityPoller.Status;
			}
			priorityWorking = true;
		}
		
		/// <summary>
		/// Event: occurs when an item is not found within the result.
        /// This is relevant when using the hashing feature within a product.
		/// </summary>
		/// <param name="o">o</param>
		/// <param name="e">e</param>
		public void OnItemNotFound(object o, HashItemSearchEventArgs e)
		{
			if(mode == HashMode.Hash)
			{
                if (logger != null)
                    logger.Info("Item was not found. Initiating priorityPoller to find it.");
                bool isactive = false;
                WildCardCollection w = new WildCardCollection(WildCardType.Extreme, e.Name);
                if (priorityRnr != null)
                {
                    if (priorityPoller != null)
                    {
                        isactive = priorityPoller.IsActive(w);
                        if (priorityRnr.IsBusy && !isactive)
                        {
                            int tries = 0;
                            while (priorityRnr.IsBusy && tries < 5)
                            {
                                priorityRnr.CancelAsync();
                                Thread.Sleep(2000); //wait a bit for the poller to stop.
                                tries++;
                            }
                        }
                    }
                }
                if (!isactive && !priorityRnr.IsBusy)
                {
                    //this.priorityPoller = new TaskPoller(w, brain.Rules.GetRule(w));
                    this.priorityPoller = new TaskPoller(w, brain.GetRule(this.Wildcard), logger);
                    this.priorityPoller.CompletedDirs = poller.CompletedDirs;
                    this.priorityPoller.HashItemFound += new HashItemFoundHandler(OnPriorityItemFound);
                    this.priorityPoller.HashCompleted += new HashCompletedHandler(OnHashCompleted);
                    this.priorityRnr.RunWorkerAsync();
                }
			}
			else
			{
                if (logger != null)
                    logger.Error("No Items found");
			}
		}
		
		/// <summary>
		/// Event: Occurs when the timer is fired. This depends on how many timers were added.
		/// </summary>
		/// <param name="o">timer object</param>
		/// <param name="e">e</param>
		protected void OnTimerFired(object o, TimerEventArgs e)
		{
			if(e != null)
			{
				switch(e.Type)
				{
					case TimerType.ResultHash:
						if(Cache.Count > 0)
						{
							lock(Cache)
							{
								Results.Add(Cache);
								Results.Status = Cache.Status;
								Cache.Clear();
								if(ItemFound != null)
									ItemFound(Results, new HashEventArgs(Cache));
							}
						}
						break;
					case TimerType.WriteOutput:
						lock(Results)
						{
							if(Results.HasChanged && mode == HashMode.Hash){
								SearchBrain.List.StoreList(this.Results, "FoundList", Wildcard.Value);
							}
						}
						break;
					case TimerType.PriorityTimeout:
						if(this.priorityRnr.IsBusy && !priorityWorking)
							this.priorityRnr.CancelAsync();
						else if(priorityWorking)
							priorityWorking = false;
						break;
                    default:
                        if (logger != null)
                            logger.Error(string.Format("Invalid Timer fired:\nType:{0}", e.Type.ToString()));
                        break;
				}
			}
		}
	}	
}
