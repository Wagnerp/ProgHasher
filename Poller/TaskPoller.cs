/*
 * Created by SharpDevelop.
 * User: marteyj
 * Date: 1/21/2009
 * Time: 3:37 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using Logger;
using System.IO;
using System.Threading;
using ProgHasher.Brain;
using System.ComponentModel;
using ProgHasher.Components;
using ProgHasher.Poller.Tasks;
using ProgHasher.Components.Timer;
using System.Collections.Generic;
using ProgHasher.Components.Events;
using System.Text.RegularExpressions;


namespace ProgHasher.Poller
{
    internal partial class TaskPoller
    {
        public event HashCompletedHandler HashCompleted;
        public event HashPollCompletedHandler HashPollCompleted;
        public event HashItemFoundHandler HashItemFound;
        private ProgTimer pollTimer = null;
        private ILog logger = null;

        private const int MAX_THREAD_COUNT = 5;
        private int THREAD_COUNT = 0;
        private int THREAD_INDEX = 0;
        private int numExecuted = 0;

        public bool IgnoreSubDirectories = false;
        public bool IgnoreDepthRestriction = false;
        public bool SearchHiddenFolders = true;

        public System.Collections.Generic.List<string> Results = null;
        public System.Collections.Generic.List<string> CompletedDirs = null;
        internal ProcessList Poll = null;

        public String Status = string.Empty;

        private Rule[] Rules = null;

        internal ManualResetEvents Events = null;
        private ManualResetEvents tmpEvents = null;

        private long TakenCount = 0;
        private WildCardCollection wildcards = null;
        private WildCardCollection locations = null;

        /// <summary>
        /// Default ctor
        /// </summary>
        /// <param name="wildcards"></param>
        internal TaskPoller(WildCardCollection wildcards, Rule[] rules, ILog logger)
        {
            this.logger = logger;
            Thread.CurrentThread.IsBackground = true;
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
            this.Rules = rules;
            this.Events = new ManualResetEvents();
            this.tmpEvents = new ManualResetEvents();
            this.pollTimer = new ProgTimer(TimerType.HashDelay);
            this.pollTimer.TimerFired += new HashTimerEventHandler(pollTimer_TimerFired);
            this.wildcards = wildcards;
            Poll = new ProcessList();
            CompletedDirs = new List<string>();
            Results = new List<string>();
            CreateTasks();
        }

        /// <summary>
        /// Setups the Poller with initial folders to start with.
        /// </summary>
        internal void CreateTasks()
        {
            int i = 0;
            if (!useLocation)
            {
                this.IgnoreSubDirectories = false;

                //Console.WriteLine("Number of rules: {0}", Rules.Length);
                foreach (Rule r in Rules)
                {
                    //TODO: Work on exclusion list.
                    TaskItemList list = r["HASHFIRST"];

                    //Console.WriteLine("RuleName={0};    RuleValues:{1}", r.Name, r.wildcards.Value);

                    foreach (TaskItem taskitem in list)
                    {
                        if (taskitem != null)
                        {
                            Events.Add(new ManualResetEvent(false));
                            Task task = new Task(taskitem, wildcards, Hasher.MaxPerFolder, Events[i]);
                            task.MaxDepth = taskitem.MaxDepth;
                            //Console.WriteLine("MaxDepth = {0}", task.MaxDepth);
                            Poll.Add(task);
                            i++;
                        }
                    }
                    pollTimer.BeginTimer();
                }
            }
            else
            {
                foreach (string s in this.locations)
                {
                    if (s.Trim().CompareTo("") != 0)
                    {
                        Events.Add(new ManualResetEvent(false));
                        Poll.Add(new Task(new TaskItem(s), wildcards, Hasher.MaxPerFolder, Events[i]));
                        i++;
                    }
                }
            }
        }

        /// <summary>
        /// Starts the Polling and searching process.
        /// </summary>
        /// <param name="bwg"></param>
        internal void InitRequest(BackgroundWorker bwg)
        {
            bool allowbreak = false;
            while ((HasTasks() || this.pollTimer.IsBusy) && !bwg.CancellationPending)
            {
                while (HasTasks() && !bwg.CancellationPending)
                {
                    if (THREAD_COUNT < MAX_THREAD_COUNT)
                    {
                        try
                        {
                            Task task = NextAvailable();
                            if (task == null && pollTimer.IsBusy)
                            {
                                allowbreak = false;
                                break;
                            }
                            else if (task == null)
                            {
                                allowbreak = true;
                                break;
                            }
                            task.TaskCompleted += new TaskCompletedHandler(TaskCompleted);
                            task.DirectoryFound += new DirectoryPolledHandler(DirectoryFound);
                            task.FileAdded += new FilePolledHandler(FileFound);
                            if (logger != null)
                                logger.Info(string.Format("Initializing request to find {0} in '{1}'", this.wildcards.Value, task.location));
                            if (ThreadPool.QueueUserWorkItem(task.BeginTask, THREAD_INDEX))
                            {
                                ++THREAD_COUNT;
                                ++THREAD_INDEX;
                                ++numExecuted;
                                CompletedDirs.Add(task.location);
                            }

                            Status = string.Format(
                                "Number of directories polled = {0}.\nNumber of directories completed = {1}.\nEvents = {2}",
                                    Poll.Count, CompletedDirs.Count, Events.Count);
                            if (logger != null)
                                logger.Info(Status);
                        }
                        catch (System.Exception e)
                        {
                            if (logger != null)
                                logger.Error(e);
                            //throw e;
                        }
                    }
                }
                if (allowbreak)
                    break;
                Thread.Sleep(5000);  //wait 5 seconds for tasks to start.
            }
            OnHashPollCompleted(new HashEventArgs(this.wildcards));
            if (!bwg.CancellationPending)
                WaitAll(0);
            else
                WaitAll(1);
        }

        /// <summary>
        /// Issues a Wait on all active threads. Issues a time out if specified.
        /// default timeout: 1 minute.
        /// </summary>
        /// <param name="timeout"></param>
        internal void WaitAll(int timeout)
        {
            try
            {
                if (Events != null)
                {
                    if (timeout == 0)
                        if (WaitHandle.WaitAll(Events.ToArray(), 1000 * 3600, false))
                        {
                            if (logger != null)
                                logger.Info("wait completed");
                        }
                        else
                            if (WaitHandle.WaitAll(Events.ToArray(), timeout, false))
                            {
                                if (logger != null)
                                    logger.Info("wait completed");
                            }
                }
                OnHashCompleted(new HashEventArgs(this.wildcards));
            }
            catch (System.ArgumentNullException e)
            {
                if (logger != null)
                    logger.Info(e);
            }
        }

        /// <summary>
        /// Returns the next available task within the Poll. If noone is found, NULL is returned.
        /// </summary>
        /// <returns></returns>
        internal Task NextAvailable()
        {
            lock (Poll)
            {
                for (int i = (int)TakenCount; i < Poll.Count; i++)
                {
                    if (!Poll[i].Taken && !(Poll[i].IsSubDir && IgnoreSubDirectories) && Qualifies(Poll[i]))
                    {
                        TakenCount++;
                        Poll[i].Taken = true;
                        if (!Events.Contains(Poll[i].DoneEvent))
                        {
                            Events.Add(Poll[i].DoneEvent);
                            tmpEvents.Remove(Poll[i].DoneEvent);
                        }
                        return Poll[i];
                    }
                }
                for (int i = 0; i < TakenCount; i++)
                {
                    if (!Poll[i].Taken && !(Poll[i].IsSubDir && IgnoreSubDirectories) && Qualifies(Poll[i]))
                    {
                        TakenCount++;
                        Poll[i].Taken = true;
                        if (!Events.Contains(Poll[i].DoneEvent))
                        {
                            Events.Add(Poll[i].DoneEvent);
                            tmpEvents.Remove(Poll[i].DoneEvent);
                        }
                        return Poll[i];
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Implements depth restriction when required.
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        internal bool Qualifies(Task task)
        {
            //Console.WriteLine("MaxDepth = {0} :: depth = {1}", task.MaxDepth, task.Item.Depth);
            if (IgnoreDepthRestriction)
                return true;
            return (task.Item.Depth <= task.MaxDepth);
        }

        /// <summary>
        /// Checks if there are still untaken tasks in the Poll.
        /// returns true if so; false if not.
        /// </summary>
        /// <returns></returns>
        internal bool HasTasks()
        {
            lock (Poll)
            {
                if (Poll.Count > TakenCount)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Event: Occurs when all folders have been searched and hashing is completed.
        /// </summary>
        /// <param name="e"></param>
        public void OnHashCompleted(HashEventArgs e)
        {
            if (HashCompleted != null)
                HashCompleted(new object(), e);
        }

        /// <summary>
        /// Event: Occurs when all folders have been added to the poll and are awaiting search.
        /// </summary>
        /// <param name="e"></param>
        public void OnHashPollCompleted(HashEventArgs e)
        {
            if (HashPollCompleted != null)
                HashPollCompleted(new object(), e);
        }

        /// <summary>
        /// Event: Occurs when a single search within a directory is finished.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void TaskCompleted(object o, HashEventArgs e)
        {
            //lock(this)
            //{
            THREAD_COUNT--;
            THREAD_INDEX = e.TaskID;
            //Console.WriteLine("i finished a task");
            //}
        }

        /// <summary>
        /// Event: Occurs when a directory is found while searching for the given wildcard.
        /// The directory is polled to be searched if necessary.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void DirectoryFound(object o, HashEventArgs e)
        {
            lock (Poll)
            {
                //System.Console.WriteLine("{0}, was found and appended to queue", e.Task);
                if (!CompletedDirs.Contains(e.Task.ToLower()) && !Poll.Contains(e.Task.ToLower()) && !Exclude(e.Task))
                {
                    ManualResetEvent mre = new ManualResetEvent(false);
                    tmpEvents.Add(mre);
                    Task task = new Task((TaskItem)o, wildcards, SearchBrain.List.MaxPerFolder, mre);
                    task.IsSubDir = true;
                    Poll.Add(task);
                }
            }
        }

        /// <summary>
        /// Check if should path should be excluded.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool Exclude(string path)
        {
            try
            {

                foreach (Regex r in SearchRules.ExclusionList)
                {
                    if (r.IsMatch(path))
                    {
                        return true;
                    }
                }
                FileAttributes f = new DirectoryInfo(path).Attributes;
                if (!this.SearchHiddenFolders && ((f & FileAttributes.Hidden) == FileAttributes.Hidden))
                    return true;
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Event: Occurs when a file match is found for the given wildcard search
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void FileFound(object o, HashEventArgs e)
        {
            lock (Results)
            {
                //System.Console.WriteLine("{0}, was found as type requested", e.Task);
                if (!Results.Contains(e.Task))
                {
                    Results.Add(e.Task);
                    if (HashItemFound != null)
                        HashItemFound(Results, e);
                }
            }
        }

        /// <summary>
        /// Checks if a set of wildcards are already been searched for.
        /// </summary>
        /// <param name="wildcard"></param>
        /// <returns></returns>
        public bool IsActive(WildCardCollection wildcard)
        {
            if (this.wildcards.Contains(wildcard))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Event: Occurs when the timer is fired.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void pollTimer_TimerFired(object o, TimerEventArgs e)
        {
            this.IgnoreSubDirectories = false;
            
            pollTimer.StopTimer();

            //Console.WriteLine("Number of rules: {0}", Rules.Length);

            foreach (Rule r in Rules)
            {
                //TODO: Work on exclusion list.
                TaskItemList list = r["HASHAFTER"];
                foreach (TaskItem taskitem in list)
                {
                    lock(Poll)
                    {
                        //Console.WriteLine("RuleName={0};    RuleValues:{1}", r.Name, r.wildcards.Value);
                        if (!CompletedDirs.Contains(taskitem.Name.ToLower()) && !Poll.Contains(taskitem.Name.ToLower()) && !Exclude(taskitem.Name))
                        {
                            ManualResetEvent mre = new ManualResetEvent(false);
                            tmpEvents.Add(mre);
                            Task task = new Task(taskitem, wildcards, SearchBrain.List.MaxPerFolder, mre);
                            task.MaxDepth = taskitem.MaxDepth;
                            Poll.Add(task);
                        }
                    }
                }
            }
        }
    }
}
