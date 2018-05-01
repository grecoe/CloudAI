using ImageClassifier.Interfaces.GenericUI;
using ImageClassifier.Interfaces.GlobalUtils.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageClassifier.Interfaces.GlobalUtils.Processing
{

    class SinkPostProcess : PostProcessBase
    {
        public SinkPostProcess(IDataSink sink)
            :base(sink)
        {
        }

        public bool ItemsToProcess
        {
            get
            {
                Dictionary<String, Dictionary<String, List<ProcessItem>>> processList =
                    this.GetUpdateList(true);

                int count = 0;
                foreach (KeyValuePair<String, Dictionary<String, List<ProcessItem>>> kvp in processList)
                {
                    foreach (KeyValuePair<String, List<ProcessItem>> kvpInner in kvp.Value)
                    {
                        count += kvpInner.Value.Count;
                    }
                }

                return count != 0;
            }
        }

        public String CollectSummary()
        {
            StringBuilder sbReturnSummary = new StringBuilder();

            Dictionary<String, Dictionary<String, List<ProcessItem>>> processList =
                this.GetUpdateList(true);

            foreach(KeyValuePair<String, Dictionary<String,List<ProcessItem>>> kvp in processList)
            {
                sbReturnSummary.AppendFormat("Original Classification: {0}{1}", kvp.Key, Environment.NewLine);

                foreach(KeyValuePair<String, List<ProcessItem>> kvpInner in kvp.Value)
                {
                    sbReturnSummary.AppendFormat("\tNew Classification: {0}{1}", kvpInner.Key, Environment.NewLine);
                    sbReturnSummary.AppendFormat("\t\tItem Count: {0}{1}", kvpInner.Value.Count, Environment.NewLine);
                }
            }

            return sbReturnSummary.ToString();
        }

        /// <summary>
        /// Processes the changes. If anything fails, a false is returned.
        /// </summary>
        /// <returns></returns>
        public bool Process()
        {
            // Clear any past status
            this.Status = new List<string>();

            bool returnValue = true;

            Dictionary<String, Dictionary<String, List<ProcessItem>>> processList =
                this.GetUpdateList(false);

            AcquireContentWindow contentWindow = new AcquireContentWindow();
            contentWindow.DisplayContent = "Processing records.....";

            foreach (KeyValuePair<String, Dictionary<String, List<ProcessItem>>> kvp in processList)
            {
                this.RecordStatus(String.Format("Processing Classification : {0}", kvp.Key));

                foreach (KeyValuePair<String, List<ProcessItem>> kvpInner in kvp.Value)
                {
                    this.RecordStatus(String.Format("Moving Items: {0} -> {1}",kvp.Key, kvpInner.Key));

                    foreach(ProcessItem item in kvpInner.Value)
                    {
                        // Set flag if ANYTHING fails
                        this.RecordStatus(String.Format("\t{0}", item.Name));
                        this.RecordStatus(String.Format("\t{0} ->", item.OriginalLocation));
                        this.RecordStatus(String.Format("\t\t{0}", item.NewLocation));

                        try
                        {
                            // Because we may have files with the same name already in the new location,
                            // just make sure it's ok...
                            if(System.IO.File.Exists(item.NewLocation))
                            {
                                int attempt = 0;
                                String fileFormat = this.GetFileFormatDuplicate(item.NewLocation);
                                do
                                {
                                    item.NewLocation = String.Format(fileFormat, ++attempt);
                                } while (System.IO.File.Exists(item.NewLocation));
                            }

                            System.IO.File.Move(item.OriginalLocation, item.NewLocation);
                        }
                        catch(Exception ex)
                        {
                            returnValue = false;
                            String message = String.Format("Failed to move {0} -> {1}", item.Name, ex.Message);

                            this.Status.Add(message);
                            this.RecordStatus(message);
                        }
                    }
                }
            }

            contentWindow.Close();

            return returnValue;
        }

        #region Private Helpers

        /// <summary>
        /// Gets a list of all the items to move broken down
        /// D1:String -> Original Container
        /// D1:Dict:String -> New Container
        /// D1:Dict:List -> Items to move
        /// 
        /// Items will ONLY be included in the list IF
        ///     1. There is only 1 classificaiton
        ///     2. The classification differs from where it is now
        /// </summary>
        /// <param name="summary">
        ///     Indicates if a summary is being collected or not. If a summary, do not create
        ///     new directories, just print out what WOULD happen. 
        /// </param>
        /// <returns></returns>
        private Dictionary<String,Dictionary<String,List<ProcessItem>>>  GetUpdateList(bool summary)
        {
            Dictionary<String, Dictionary<String, List<ProcessItem>>> returnCollection = new Dictionary<String, Dictionary<String, List<ProcessItem>>>();

            IEnumerable<String> containers = this.Sink.Containers;
            foreach (String cont in containers)
            {
                // Get the friendly name of the container so we can use it as a classification
                // check against where it's supposed to go.
                String friendlyContainer = this.FriendlyNameContainerFromDirectoryName(cont);

                // Add this container to the list
                returnCollection[friendlyContainer] = new Dictionary<string, List<ProcessItem>>();

                // For each container type, collect the information
                foreach (ScoredItem item in this.Sink.GetContainerItems(cont))
                {
                    // Only allow one move if there is one classification AND
                    // that classificaiton is NOT the current classification
                    if (item.Classifications.Count == 1 &&
                        String.Compare(item.Classifications[0], friendlyContainer) != 0)
                    {
                        // Is it still there? 
                        if (System.IO.File.Exists(item.Name))
                        {
                            // Start the list of items
                            if(!returnCollection[friendlyContainer].ContainsKey(item.Classifications[0]))
                            {
                                returnCollection[friendlyContainer][item.Classifications[0]] = new List<ProcessItem>(); 
                            }

                            // item.Name holds the current location of the file
                            String newLocation = this.CreatePathToNewContainer(item.Name, item.Classifications[0], summary);

                            // Create the new item to process and add it to the list.
                            ProcessItem newItem = new ProcessItem()
                            {
                                Name = System.IO.Path.GetFileName(item.Name),
                                OriginalLocation = item.Name,
                                NewLocation = newLocation
                            };

                            returnCollection[friendlyContainer][item.Classifications[0]].Add(newItem);
                        }
                    }
                }
            }

            return returnCollection;
        }

        /// <summary>
        /// Given the path of an existing item, create a new disk path based on the new container name.
        /// </summary>
        /// <param name="exisitingPath">Existing full disk path to a file on disk</param>
        /// <param name="newContainer">New directory to place the item into</param>
        /// <param name="summary">If true, we are summarizing, if false it ensures that the new
        /// directory specified exists on disk.</param>
        /// <returns>Modified path to new location.</returns>
        private String CreatePathToNewContainer(String exisitingPath, String newContainer, bool summary)
        {
            String currentFile = System.IO.Path.GetFileName(exisitingPath);
            String currentDirectory = System.IO.Path.GetDirectoryName(exisitingPath);
            String newDirectory = String.Empty;

            int idx = currentDirectory.LastIndexOf('\\');
            if (idx == -1)
            {
                idx = currentDirectory.LastIndexOf('/');
            }

            if(idx != -1)
            {
                newDirectory = currentDirectory.Substring(0, idx);
            }
            else
            {
                throw new Exception("Something is wrong with the path in SinkPostProcess.cs");
            }

            newDirectory = System.IO.Path.Combine(newDirectory, newContainer.Trim());

            if (!summary)
            {
                FileUtils.EnsureDirectoryExists(newDirectory);
            }

            String newFile = System.IO.Path.Combine(newDirectory, currentFile);
            if(System.IO.File.Exists(newFile))
            {
                int attempt = 0;
                String fileFormat = this.GetFileFormatDuplicate(newFile);
                do
                {
                    newFile = String.Format(fileFormat, ++attempt);
                } while (System.IO.File.Exists(newFile));
            }
            return newFile;
        }

        /// <summary>
        /// If duplicate file names are found then modify the path to be file(n).ext where n is
        /// a placeholder for string.format() which typically would be an integer. 
        /// </summary>
        /// <param name="fileName">FIle name to modify the format of</param>
        /// <returns></returns>
        private string GetFileFormatDuplicate(String fileName)
        {
            String returnFileFormat = String.Empty;
            int idx = fileName.LastIndexOf('.');

            if(idx != -1)
            {
                returnFileFormat = String.Format("{0}{1}{2}",
                    fileName.Substring(0, idx),
                    "({0})",
                    fileName.Substring(idx));
            }
            else
            {
                returnFileFormat = String.Format("{0}{1}",
                    fileName,
                    "({0})");
            }

            return returnFileFormat;
        }

 
        #endregion

    }
}
