/*
 * Created by SharpDevelop.
 * User: marteyj
 * Date: 1/21/2009
 * Time: 4:44 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Threading;
using ProgHasher.Components;
using System.ComponentModel;
using ProgHasher.Poller.Tasks;
using System.Collections.Generic;
using ProgHasher.Components.Events;


namespace ProgHasher.Poller
{
	internal partial class TaskPoller
	{
		private bool useLocation = false;
		
		internal TaskPoller(WildCardCollection locations, WildCardCollection wildcards)
        {
        	this.wildcards = wildcards;
        	this.locations = locations;
        	this.Events = new ManualResetEvents();
			Poll = new ProcessList();
			CompletedDirs = new List<string>();
			Results = new List<string>();
			useLocation = true;
        }	
	}
}
