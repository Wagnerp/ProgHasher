/*
 * Created by SharpDevelop.
 * User: marteyj
 * Date: 1/21/2009
 * Time: 3:55 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using Logger;
using System.Threading;
using ProgHasher.Components;
using System.ComponentModel;
using System.Collections.Generic;


namespace ProgHasher
{
	public class Hasher : HasherBase
	{	
		public WildCardCollection Locations = null;
				
		public Hasher(string wildcard, long size) : base()
		{
			this.Wildcard = new WildCardCollection(wildcard);
			this.Size = size;
			ValidateRequest();
		}
		
		public Hasher(string wildcard, string location, long size) : base()
		{
			this.Locations = new WildCardCollection(location);
			this.Wildcard = new WildCardCollection(wildcard);
			this.Size = size;
			ValidateRequest();
		}
		
		public Hasher(WildCardCollection wildcard, WildCardCollection location, long size) : base()
		{
			this.Locations = location;
			this.Wildcard = wildcard;
			this.Size = size;
			ValidateRequest();
		}

        public Hasher(WildCardCollection wildcard, WildCardCollection location, long size, ILog logger)
            : base(logger)
        {
            this.Locations = location;
            this.Wildcard = wildcard;
            this.Size = size;
            ValidateRequest();
        }
		
		public Hasher(string wildcard, HashSize size, ILog logger) : base(logger)
		{
			this.Wildcard = new WildCardCollection(wildcard);
			this.GetSize(size);
			ValidateRequest();
		}
		
		public Hasher(WildCardCollection wildcard, HashSize size) : base()
		{
			this.Wildcard = wildcard;
			this.GetSize(size);
			ValidateRequest();
		}

        public Hasher(WildCardCollection wildcard, HashSize size, ILog logger)
            : base(logger)
        {
            this.Wildcard = wildcard;
            this.GetSize(size);
            ValidateRequest();
        }
		
		public Hasher(WildCardCollection wildcard, long size) : base()
		{
			this.Wildcard = wildcard;
			this.Size = size;
			ValidateRequest();
		}

        public Hasher(WildCardCollection wildcard, long size, ILog logger)
            : base(logger)
        {
            this.Wildcard = wildcard;
            this.Size = size;
            ValidateRequest();
        }
	}
}
