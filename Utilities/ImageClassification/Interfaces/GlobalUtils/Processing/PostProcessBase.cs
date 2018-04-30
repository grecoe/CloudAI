using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageClassifier.Interfaces.GlobalUtils.Processing
{
    class ProcessItem
    {
        public String Name { get; set; }
        public String OriginalLocation { get; set; }
        public String NewLocation { get; set; }
    }

    class PostProcessBase
    {
        protected IDataSink Sink { get; set; }

        public List<string> Status { get; protected set; }

        public PostProcessBase(IDataSink sink)
        {
            this.Sink = sink;
            this.Status = new List<string>();
        }

        /// <summary>
        /// Dump out a message to the console for now.
        /// </summary>
        /// <param name="message"></param>
        protected void RecordStatus(String message)
        {
            Console.WriteLine(message);
        }

        /// <summary>
        /// Container names are a full path. Get the last directory part of the path
        /// as that will be the current classification.
        /// </summary>
        /// <param name="container">A directory path without the file extension</param>
        /// <returns></returns>
        protected string FriendlyNameContainerFromDirectoryName(string container)
        {
            string returnValue = string.Empty;

            int idx = container.LastIndexOf('\\');
            if (idx == -1)
            {
                idx = container.LastIndexOf('/');
            }

            if (idx == -1)
            {
                returnValue = container;
            }
            else
            {
                returnValue = container.Substring(idx + 1);

                // Account for storage labelled as well
                if(String.IsNullOrEmpty(returnValue))
                {
                    returnValue = container.Substring(0, idx);
                }
            }

            return returnValue;
        }

    }
}
