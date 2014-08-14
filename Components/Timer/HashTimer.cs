/*
 * Created by SharpDevelop.
 * User: marteyj
 * Date: 1/22/2009
 * Time: 10:31 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using Logger;
using System.Threading;
using System.ComponentModel;
using System.Collections.Generic;
using ProgHasher.Components.Events;

namespace ProgHasher.Components.Timer
{
	/// <summary>
	/// Description of HashTimer.
	/// </summary>
	public class HashTimer
	{
		public event HashTimerEventHandler TimerFired;
		
		public int Interval = 1000;
		
		/// <summary>
		/// Time Elapsed since hash started in milliseconds.
		/// </summary>
        public System.Int64 TimeElapsed
        {
            get
            {
                return timeElapsed;
            }
        }

        private System.Int64 timeElapsed = 0L;

		public TimerType[] Types;
		
		private Dictionary<TimerType, long> times;
		public HashTimer(params TimerType[] types)
		{
			Thread.CurrentThread.Priority = ThreadPriority.Highest;
			times = new Dictionary<TimerType, long>();
			this.Types = types;
			this.GetTimes();
		}
		
        /// <summary>
        /// ctor
        /// </summary>
		public HashTimer()
		{
			Thread.CurrentThread.Priority = ThreadPriority.Highest;
			times = new Dictionary<TimerType, long>();
		}
		
        /// <summary>
        /// 
        /// </summary>
		private void GetTimes()
		{
			if(Types != null)
			{
				foreach(TimerType timertype in Types)
				{
					string name = Enum.GetName(typeof(TimerType), timertype);
					times[timertype] = (long)Enum.Parse(typeof(TimerType), name);
				}
			}
		}
		
        /// <summary>
        /// Add a timer to the list of timers.
        /// </summary>
        /// <param name="type"></param>
		public void AddTimer(TimerType type)
		{
			string name = Enum.GetName(typeof(TimerType), type);
			times[type] = (long)Enum.Parse(typeof(TimerType), name);
		}
		
        /// <summary>
        /// Remove a timer from the list of timers.
        /// </summary>
        /// <param name="type"></param>
		public void RemoveTimer(TimerType type)
		{
            lock (times) { times.Remove(type); }
            
		}
		
        /// <summary>
        /// Start the timer.
        /// </summary>
        /// <param name="bwg"></param>
		public void BeginTimer(BackgroundWorker bwg)
		{						
			while(!bwg.CancellationPending)
			{
                Dictionary<TimerType, long> values = new Dictionary<TimerType,long>(times);
                foreach(KeyValuePair<TimerType, long> kvp in values)
				{
					if(!((kvp.Key == TimerType.WriteOutput || kvp.Key == TimerType.PriorityTimeout || kvp.Key == TimerType.HashDelay)
                        && (timeElapsed == 0)))
					{
						if((timeElapsed % kvp.Value).CompareTo(0) == 0 )
						{
							if(TimerFired != null)
								TimerFired(this, new TimerEventArgs(kvp.Key));
						}
					}
				}
				
				System.Threading.Thread.Sleep(Interval);
				this.timeElapsed += 1000;
			}
		}
	}
	
    /// <summary>
    /// Timer types
    /// </summary>
	public enum TimerType : long
	{
		ResultHash = 5000,
		//WriteOutput = 20*3600*1000,
		WriteOutput = 7*1000,
		PriorityTimeout = 6 * 1000,
        HashDelay = 10 * 1000,
	}

    /// <summary>
    /// Timer wrapper class.
    /// </summary>
    public class ProgTimer
    {
        private BackgroundWorker timer = null;
        private HashTimer hashTimer = null;
        public ILog logger = null;

        /// <summary>
        /// Event: Occurs when the timer is fired.
        /// </summary>
        public event HashTimerEventHandler TimerFired;

        public ProgTimer( ILog logger, params TimerType[] timerTypes)
        {
            this.timer = new BackgroundWorker();
            this.timer.WorkerSupportsCancellation = true;
            this.timer.DoWork += new DoWorkEventHandler(this.BGWTimerInit);
            this.timer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BWGTimerCompleted);

            if (timerTypes != null && timerTypes.Length > 0)
            {
                hashTimer = new HashTimer(timerTypes);
            }
            else
                hashTimer = new HashTimer();
            hashTimer.TimerFired += new HashTimerEventHandler(OnTimerFired);

        }

        public ProgTimer(params TimerType[] timerTypes)
        {
            this.timer = new BackgroundWorker();
            this.timer.WorkerSupportsCancellation = true;
            this.timer.DoWork += new DoWorkEventHandler(this.BGWTimerInit);
            this.timer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BWGTimerCompleted);

            if (timerTypes != null && timerTypes.Length > 0)
            {
                hashTimer = new HashTimer(timerTypes);
            }
            else
                hashTimer = new HashTimer();
            hashTimer.TimerFired += new HashTimerEventHandler(OnTimerFired);

        }

        /// <summary>
        /// Event: occurs when the timer is fired.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void OnTimerFired(object o, TimerEventArgs e)
        {
            if (TimerFired != null)
                TimerFired(o, e);
        }

        /// <summary>
        /// Initiate the timer.
        /// </summary>
        public void BeginTimer()
        {
            if (!timer.IsBusy)
                timer.RunWorkerAsync();
        }

        /// <summary>
        /// Stop the timer.
        /// </summary>
        public void StopTimer()
        {
            if (timer.IsBusy)
                timer.CancelAsync();
        }

        /// <summary>
        /// Start the background timer process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BGWTimerInit(object sender, DoWorkEventArgs e)
        {
            if (sender != null)
            {
                BackgroundWorker bw = sender as BackgroundWorker;
                hashTimer.BeginTimer(bw);
                if (bw.CancellationPending)
                {
                    e.Cancel = true;
                }
            }
        }

        /// <summary>
        /// return the status of the timer.
        /// </summary>
        public bool IsBusy
        {
            get
            {
                return timer.IsBusy;
            }
        }

        /// <summary>
        /// Gets a value determining if the application has requested termination of background operation.
        /// </summary>
        public bool StopPending
        {
            get
            {
                return timer.CancellationPending;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public long TimeElapsed
        {
            get { return hashTimer.TimeElapsed; }
        }

        /// <summary>
        /// Adds a timer object to the list of timers.s
        /// </summary>
        /// <param name="timerType"></param>
        public void AddTimer(TimerType timerType)
        {
            hashTimer.AddTimer(timerType);
        }

        /// <summary>
        /// Removes a timer from the list of timers.
        /// </summary>
        /// <param name="timerType"></param>
        public void RemoveTimer(TimerType timerType)
        {
            hashTimer.RemoveTimer(timerType);
        }


        /// <summary>
        /// When the timer stops. Verify the cause.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BWGTimerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                // The user canceled the operation.
                if(logger != null)
                    logger.Debug("Timer was canceled");
            }
            else if (e.Error != null)
            {
                // There was an error during the operation.
                string msg = String.Format("An error occurred: {0}", e.Error);
                if(logger != null)
                    logger.Error(msg);
                timer.RunWorkerAsync();
            }
            else
            {
                logger.Info("Timer Finished Successfully");
            }
        }
    }
}
