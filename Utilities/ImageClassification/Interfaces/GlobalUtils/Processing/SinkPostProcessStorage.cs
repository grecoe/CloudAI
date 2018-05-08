using ImageClassifier.Interfaces.GenericUI;
using ImageClassifier.Interfaces.GlobalUtils.AzureStorage;
using ImageClassifier.Interfaces.GlobalUtils.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageClassifier.Interfaces.GlobalUtils.Processing
{
    class UrlDisassebled
    {
        public String BaseUrl { get; set; }
        public List<string> Directories { get; set; }
        public String FileName {get ;set;}

        public UrlDisassebled()
        {
            this.Directories = new List<string>();
        }

        public String BlobPath
        {
            get
            {
                List<string> parts = new List<string>(this.Directories);
                parts.Add(this.FileName);

                return String.Join("/", parts);
            }
        }
    }

    class SinkPostProcessStorage : PostProcessBase
    {
        private AzureBlobStorageConfiguration Configuration { get; set; }

        public SinkPostProcessStorage(IDataSink sink, AzureBlobStorageConfiguration config)
            : base(sink)
        {
            this.Configuration = config;
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

            foreach (KeyValuePair<String, Dictionary<String, List<ProcessItem>>> kvp in processList)
            {
                sbReturnSummary.AppendFormat("Original Classification: {0}{1}", kvp.Key, Environment.NewLine);

                foreach (KeyValuePair<String, List<ProcessItem>> kvpInner in kvp.Value)
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
        public async Task<bool> Process()
        {
            // Clear any past status
            this.Status = new List<string>();

            bool returnValue = true;

            Dictionary<String, Dictionary<String, List<ProcessItem>>> processList =
                this.GetUpdateList(false);

            AcquireContentWindow contentWindow = new AcquireContentWindow(null);
            contentWindow.DisplayContent = "Processing records.....";
            contentWindow.Show();

            StorageUtility azureStorageUtility = new StorageUtility(this.Configuration);

            foreach (KeyValuePair<String, Dictionary<String, List<ProcessItem>>> kvp in processList)
            {
                this.RecordStatus(String.Format("Processing Classification : {0}", kvp.Key));

                foreach (KeyValuePair<String, List<ProcessItem>> kvpInner in kvp.Value)
                {
                    this.RecordStatus(String.Format("Moving Items: {0} -> {1}", kvp.Key, kvpInner.Key));

                    foreach (ProcessItem item in kvpInner.Value)
                    {
                        // Set flag if ANYTHING fails
                        this.RecordStatus(String.Format("\t{0}", item.Name));
                        this.RecordStatus(String.Format("\t{0} ->", item.OriginalLocation));
                        this.RecordStatus(String.Format("\t\t{0}", item.NewLocation));


                        UrlDisassebled sourceBlob = this.DisassebleUrl(item.OriginalLocation);
                        UrlDisassebled destinationBlob = this.DisassebleUrl(item.NewLocation);

                        String message = String.Empty;
                        try
                        {
                            bool result = await azureStorageUtility.MoveBlob(sourceBlob.BlobPath, destinationBlob.BlobPath);
                            if(!result)
                            {
                                message = String.Format("Failed to move {0}", item.Name);
                            }
                        }
                        catch(Exception ex)
                        {
                            message = String.Format("Failed to move {0} -> {1}", item.Name, ex.Message);
                        }

                        if(!String.IsNullOrEmpty(message))
                        {
                            returnValue = false;
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
        private Dictionary<String, Dictionary<String, List<ProcessItem>>> GetUpdateList(bool summary)
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
                        // Start the list of items
                        if (!returnCollection[friendlyContainer].ContainsKey(item.Classifications[0]))
                        {
                            returnCollection[friendlyContainer][item.Classifications[0]] = new List<ProcessItem>();
                        }

                        // Break down the URL and build the base to it.
                        UrlDisassebled disassebled = this.DisassebleUrl(item.Name);
                        String newUrlLocation = disassebled.BaseUrl;
                        if(!String.IsNullOrEmpty(this.Configuration.BlobPrefix))
                        {
                            string prefix = this.Configuration.BlobPrefix.Trim(new char[] { '/' });
                            newUrlLocation = String.Format("{0}/{1}", newUrlLocation, prefix);
                        }

                        // Add on the part of the path that identifies the new location
                        newUrlLocation = String.Format("{0}/{1}/{2}", newUrlLocation, item.Classifications[0], disassebled.FileName);


                        // Create the new item to process and add it to the list.
                        ProcessItem newItem = new ProcessItem()
                        {
                            Name = System.IO.Path.GetFileName(item.Name),
                            OriginalLocation = item.Name,
                            NewLocation = newUrlLocation
                        };

                        returnCollection[friendlyContainer][item.Classifications[0]].Add(newItem);
                    }
                }
            }

            return returnCollection;
        }

        /// <summary>
        /// Breaks down the URL of the original item so we can rebuild a new path to it in storage.
        /// </summary>
        /// <param name="originalUrl"></param>
        /// <returns></returns>
        private UrlDisassebled DisassebleUrl(String originalUrl)
        {
            UrlDisassebled returnValue = new UrlDisassebled();
            int idxContainer = originalUrl.IndexOf(this.Configuration.StorageContainer) + this.Configuration.StorageContainer.Length;

            // Get the file name
            returnValue.FileName = System.IO.Path.GetFileName(originalUrl);
            // Get the URL to account/container
            returnValue.BaseUrl = originalUrl.Substring(0, idxContainer);

            // Split up the rest of it as it is directory paths
            string remainder = originalUrl.Substring(idxContainer + 1);
            returnValue.Directories = new List<string>(remainder.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
            returnValue.Directories.Remove(returnValue.FileName);

            return returnValue;
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

            if (idx != -1)
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
