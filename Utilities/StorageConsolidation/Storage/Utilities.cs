//
// Copyright  Microsoft Corporation ("Microsoft").
//
// Microsoft grants you the right to use this software in accordance with your subscription agreement, if any, to use software 
// provided for use with Microsoft Azure ("Subscription Agreement").  All software is licensed, not sold.  
// 
// If you do not have a Subscription Agreement, or at your option if you so choose, Microsoft grants you a nonexclusive, perpetual, 
// royalty-free right to use and modify this software solely for your internal business purposes in connection with Microsoft Azure 
// and other Microsoft products, including but not limited to, Microsoft R Open, Microsoft R Server, and Microsoft SQL Server.  
// 
// Unless otherwise stated in your Subscription Agreement, the following applies.  THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT 
// WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL MICROSOFT OR ITS LICENSORS BE LIABLE 
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED 
// TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THE SAMPLE CODE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
//

using System;
using System.Collections.Generic;

namespace StorageConsolidation.Storage
{
    /// <summary>
    /// Helper class to break apart the connection string to an Azure Storage Account
    /// </summary>
    class ConnectionStringParser
    {
        public static string ACCOUNT_NAME = "AccountName";
        public static string ACCOUNT_KEY = "AccountKey";


        public static Dictionary<String,String> ParseConnectionString(String connectionString)
        {
            Dictionary<String, String> returnValue = new Dictionary<string, string>();

            string[] parts = connectionString.Split(new char[] { ';' });
            foreach(string part in parts)
            {
                int components = part.IndexOf('=');
                returnValue.Add(part.Substring(0, components), part.Substring(components + 1));
            }

            return returnValue;
        }
    }

    /// <summary>
    /// Helper class to create a new unique location name. Names are built in the form
    /// 
    ///     prepend-append
    ///     
    /// However, if the name exists, the CreateLocationName will iterate with versions of 
    /// 
    ///     prepend-append-N
    ///     
    /// Where n=2 or higher until a free location name can be found in the storage account.
    /// </summary>
    class StorageLocationName
    {
        /// <summary>
        /// Maximum name length for a container or file share is 63 characters long
        /// </summary>
        private const int MAX_NAME_LENGTH = 63;

        /// <summary>
        /// Creates a unique blob container or file share name for the storage account.
        /// </summary>
        /// <param name="storageAccount">Storage account to get a unique name for</param>
        /// <param name="locationType">Blob Storage or File Share enum</param>
        /// <param name="prepend">First part of the location name</param>
        /// <param name="append">Second part of the location name</param>
        /// <returns>A unique blob container or file share name for the given account</returns>
        public static String CreateLocationName(AzureStorage storageAccount, StorageType locationType, String prepend, String append)
        {
            int attempt = 2;
            String name = GetLocationName(prepend, append);
            while(storageAccount.StorageLocationExists(locationType, name))
            {
                name = GetLocationName(prepend, String.Format("{0}-{1}", append, attempt++));
            }

            // Have a unique name now
            return name;
        }

        #region Private Method
        /// <summary>
        /// Build the name prepend-append
        /// </summary>
        /// <param name="prepend">Typically the source storage account name</param>
        /// <param name="append">The name of the original container/fileshare</param>
        /// <returns></returns>
        private static String GetLocationName(String prepend, String append)
        {
            int length = prepend.Length + append.Length + 1;
            String newName = String.Empty;
            if (length > StorageLocationName.MAX_NAME_LENGTH)
            {
                String tempContainerName = append.Substring((length - StorageLocationName.MAX_NAME_LENGTH));
                if (tempContainerName.StartsWith("-"))
                {
                    tempContainerName = tempContainerName.Substring(1);
                }
                newName = String.Format("{0}-{1}", prepend, tempContainerName);
            }
            else
            {
                newName = String.Format("{0}-{1}", prepend, append);
            }

            return newName;
        }
        #endregion 
    }
}
