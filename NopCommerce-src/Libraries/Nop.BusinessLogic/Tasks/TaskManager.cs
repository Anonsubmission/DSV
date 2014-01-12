﻿//------------------------------------------------------------------------------
// The contents of this file are subject to the nopCommerce Public License Version 1.0 ("License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at  http://www.nopCommerce.com/License.aspx. 
// 
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. 
// See the License for the specific language governing rights and limitations under the License.
// 
// The Original Code is nopCommerce.
// The Initial Developer of the Original Code is NopSolutions.
// All Rights Reserved.
// 
// Contributor(s): _______. 
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.ObjectModel;

namespace NopSolutions.NopCommerce.BusinessLogic.Tasks
{
    /// <summary>
    /// Represents task manager
    /// </summary>
    public partial class TaskManager
    {
        private static readonly TaskManager _taskManager = new TaskManager();
        private List<TaskThread> _taskThreads = new List<TaskThread>();

        private TaskManager()
        {
        }

        internal void ProcessException(Task task, Exception exception)
        {
            try
            {
                //process exception code here
            }
            catch
            {
            }
        }

        /// <summary>
        /// Initializes the task manager with the property values specified in the configuration file.
        /// </summary>
        /// <param name="configFile">Configuration file</param>
        /// <param name="nodePath">Node path</param>
        public void Initialize(string configFile, string nodePath)
        {
            var document = new XmlDocument();
            document.Load(configFile);
            Initialize(document.SelectSingleNode(nodePath));
        }

        /// <summary>
        /// Initializes the task manager with the property values specified in the configuration file.
        /// </summary>
        /// <param name="node">Node</param>
        public void Initialize(XmlNode node)
        {
            this._taskThreads.Clear();
            foreach (XmlNode node1 in node.ChildNodes)
            {
                if (node1.Name.ToLower() == "thread")
                {
                    var taskThread = new TaskThread(node1);
                    this._taskThreads.Add(taskThread);
                    foreach (XmlNode node2 in node1.ChildNodes)
                    {
                        if (node2.Name.ToLower() == "task")
                        {
                            var attribute = node2.Attributes["type"];
                            var taskType = Type.GetType(attribute.Value);
                            if (taskType != null)
                            {
                                var task = new Task(taskType, node2);
                                taskThread.AddTask(task);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Starts the task manager
        /// </summary>
        public void Start()
        {
            foreach (var taskThread in this._taskThreads)
            {
                taskThread.InitTimer();
            }
        }

        /// <summary>
        /// Stops the task manager
        /// </summary>
        public void Stop()
        {
            foreach (var taskThread in this._taskThreads)
            {
                taskThread.Dispose();
            }
        }

        /// <summary>
        /// Gets the task mamanger instance
        /// </summary>
        public static TaskManager Instance
        {
            get
            {
                return _taskManager;
            }
        }

        /// <summary>
        /// Gets a list of task threads of this task manager
        /// </summary>
        public IList<TaskThread> TaskThreads
        {
            get
            {
                return new ReadOnlyCollection<TaskThread>(this._taskThreads);
            }
        }
    }
}
