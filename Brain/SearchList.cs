/*
 * Created by SharpDevelop.
 * User: marteyj
 * Date: 1/8/2009
 * Time: 9:40 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;
using System.Xml;
using System.Text;
using ProgHasher.Components;
using ProgHasher.Configuration;
using System.Collections.Generic;
using Logger;

namespace ProgHasher.Brain
{
    /// <summary>
    /// Description of SearchList.
    /// </summary>
    public class SearchList
    {
        private FileInfo fileInfo = null;
        public HasherConfig Config = null;
        private ILog logger = null;

        /// <summary>
        /// Default ctor
        /// </summary>
        public SearchList()
        {
            this.logger = LogManager.GetLogger(this.GetType());
            Config = new HasherConfig(logger);
            this.MaxPerFolder = 25;
            ValidateConfig();
            LoadConfigs();
        }


        public long Count
        {
            get { return this.HashList.Size; }
        }


        public HasherList HashList { get { return Config.Locations; } }

        /// <summary>
        /// Max number of items to load per folder.
        /// This is a configuration value.
        /// </summary>
        public int MaxPerFolder = 0;

        /// <summary>
        /// Load configurations within the configuration file.
        /// </summary>
        private void LoadConfigs()
        {
            try
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(fileInfo.FullName);
                XmlNode node = xDoc.SelectSingleNode("Configuration/Programs/ProgHasher/maxperfolder");
                string maxNumPerFolder = node.Value;
                try
                {
                    this.MaxPerFolder = System.Convert.ToInt32(maxNumPerFolder.ToString());
                }
                catch { }
            }
            catch (System.Exception e)
            {
                if(logger != null)
                    logger.Error(e.Message, e);
            }
        }

        /// <summary>
        /// Ensure configuration file can be opened.
        /// </summary>
        private void ValidateConfig()
        {
            if (ConfigurationManager.ConfigLocation != null)
            {
                try
                {
                    fileInfo = new FileInfo(ConfigurationManager.ConfigLocation);
                }
                catch (Exception e)
                {
                    logger.Error("Problems opening file.", e);
                }
            }
        }

        /// <summary>
        /// Store a result list in a desired format.
        /// </summary>
        /// <param name="list">the list of results to store</param>
        /// <param name="name">the name of the node to store within</param>
        /// <param name="type">the type/wildcard asociated with the results being stored.</param>
        public void StoreList(ResultList list, string name, string type)
        {
            //TODO: store properly by extension name.

            FileStream fs = ConfigurationManager.CreateOutputStream();
            StreamReader sr = new StreamReader(fs);
            XmlDocument doc = new XmlDocument();
            bool retry = false;
            try
            {
                string current = sr.ReadToEnd();
                string storednodes = string.Empty;
                sr.Close();
                if (current.Trim().CompareTo("") != 0)
                {
                    int tries = 0;
                trykeep: ;
                    try
                    {

                        XmlDocument oldDoc = new XmlDocument();
                        oldDoc.Load(ConfigurationManager.OutputLocation);
                        string query = string.Format("Storage/ProgHasher/*[@name != '{0}' and @type='{0}']", name, type.Trim());
                        storednodes = oldDoc.SelectSingleNode(query).OuterXml;
                    }
                    catch (Exception e)
                    {
                        tries++;
                        if (tries < 2)
                            goto trykeep;
                        if(logger != null)
                            logger.Error(e);
                    }
                }
                string xml = CreateXml(list, name, type, storednodes);
                doc.LoadXml(xml);
                doc.Save(ConfigurationManager.OutputLocation);
            }
            catch (System.IO.IOException e)
            {
                Console.WriteLine(e);
                retry = true;
            }
            finally
            {
                if (retry)
                    doc.Save(ConfigurationManager.OutputLocation);
            }
        }

        /// <summary>
        /// Creates a list of xml nodes.
        /// </summary>
        /// <param name="name">the name of the nodes</param>
        /// <param name="values">The list of values to use for the nodes</param>
        /// <returns></returns>
        private string CreateXmlNodeList(string name, System.Collections.Generic.List<string> values)
        {
            StringBuilder sb = new StringBuilder();
            Dictionary<string, string> item = new Dictionary<string, string>();

            foreach (string s in values)
            {
                item.Add("name", name);
                sb.AppendLine(CreateXmlNode("Item", item, true, s));
                item.Clear();
            }
            return sb.ToString();
        }

        /// <summary>
        /// Creates an xml node based on given values.
        /// </summary>
        /// <param name="name">name of the node</param>
        /// <param name="attributes">list of attributes and values.</param>
        /// <param name="hasText">true if the node has innerText; false otherwise</param>
        /// <param name="text">the innertext of the node</param>
        /// <returns>the created node string.</returns>
        private string CreateXmlNode(string name, Dictionary<string, string> attributes, bool hasText, string text)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("<{0}", name);
            int i = 0;
            if (attributes != null)
            {
                sb.Append(" ");
                foreach (string attr in attributes.Keys)
                {
                    i++;
                    sb.AppendFormat("{0}=\"{1}\"", attr, attributes[attr]);
                    if (i < attributes.Count)
                        sb.Append(" ");
                }
            }
            sb.Append(">");
            if (hasText)
            {
                sb.Append(text);
                sb.AppendFormat("</{0}>", name);
            }
            else
                sb.Append("/>");
            return sb.ToString();
        }


        /// <summary>
        /// creates an xml header.
        /// </summary>
        /// <returns></returns>
        private string CreateHeader()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            return sb.ToString();
        }

        /// <summary>
        /// Creates an xml document for hashing results from hash or search.
        /// </summary>
        /// <param name="list">list of items to store in xml</param>
        /// <param name="name">name of the node to store within</param>
        /// <param name="type">the type, wildcard involved in search.</param>
        /// <param name="appendnodes">the nodes to append within the document to preserve structure.</param>
        /// <returns>created xml document</returns>
        private string CreateXml(ResultList list, string name, string type, string appendnodes)
        {
            StringBuilder sb = new StringBuilder();
            string date = DateTime.Now.ToString("dd/MM/yyyy");
            string root = "<Storage>";
            string rootend = "</Storage>";
            string progroot = "<ProgHasher>";
            string progrootend = "</ProgHasher>";
            string itemroot = "<" + name + " name=\"" + name + "\" type=\"" + type + "\" datecached=\"" + date + "\">";
            string itemrootend = "</" + name + ">";

            sb.AppendLine(CreateHeader());
            sb.AppendLine(root);
            sb.AppendLine(progroot);
            sb.AppendLine(itemroot);
            if (list != null)
            {
                foreach (KeyValuePair<string, List> kvp in list)
                {
                    sb.AppendLine(CreateXmlNodeList(kvp.Key, kvp.Value));
                }
            }
            sb.AppendLine(itemrootend);
            sb.AppendLine(appendnodes);
            sb.AppendLine(progrootend);
            sb.AppendLine(rootend);

            return sb.ToString();
        }

        /// <summary>
        /// Loads cached search or hash items if they have been hashed recently.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public ResultList LoadPriorCachedList(string name, string type)
        {
            ResultList results = new ResultList();
            string cacheLoc = ConfigurationManager.OutputLocation;
            XmlDocument xDoc = new XmlDocument();
            try
            {
                xDoc.Load(cacheLoc);
                string xPathString = string.Format("Storage/ProgHasher/{0}[@name = '{0}' and @type = '{1}']/*", name, type);
                XmlNodeList nodes = xDoc.SelectNodes(xPathString);

                foreach (XmlNode x in nodes)
                {
                    results.SetList(x.Attributes["name"].Value, x.InnerText);
                    System.IO.FileInfo fi = new FileInfo(x.InnerText);
                    results.SetList("completeddirs", fi.Directory.FullName);
                }

            }
            catch (System.Exception e)
            {
                if(logger != null)
                    logger.Error(e.Message, e);
            }
            
            return results;
        }
    }
}
