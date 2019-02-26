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

using StorageConsolidation.Config;
using StorageConsolidation.Storage;
using System;
using System.Collections.Generic;

namespace StorageConsolidation
{
    /// <summary>
    /// Using the configuration, gets a list of all of the blob containers and file shares in the source accounts.
    /// 
    /// Creates containers/file shares in the destination accounts with the pattern
    ///     [SourceStorageAccount]-[Original container/share name]
    ///     
    /// Creates an output file that has the AzCopy commands to copy the data from the source account to the destination account.
    /// </summary>
    class Program
    {
        /// <summary>
        /// AzCopy copy command to copy either a blob container or file share content from one account to another.
        /// </summary>
        private static String STORAGE_COPY_COMMAND = "AzCopy /Source:{0}/ /Dest:{1}/ /SourceKey:{2} /DestKey:{3} /S";

        static void Main(string[] args)
        {
            Configuration configuration = Configuration.LoadConfiguration();

            AzureStorage destinationStorage = new AzureStorage(configuration.Destination);

            List<String> azCopyCommands = new List<string>();

            foreach (String conn in configuration.Sources)
            {
                // General storage account
                AzureStorage sourceStorage = new AzureStorage(conn);
                azCopyCommands.AddRange(PrepareStorageLocationCopy(sourceStorage, destinationStorage, StorageType.BlobContainer));
                azCopyCommands.AddRange(PrepareStorageLocationCopy(sourceStorage, destinationStorage, StorageType.FileShare));
            }

            String azcopypath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "azcopycommands.bat");
            System.IO.File.WriteAllText(azcopypath, String.Join(Environment.NewLine, azCopyCommands));

            Console.WriteLine(String.Format("AzCopy commands located at : {0}", azcopypath));
            Console.ReadLine();
        }

        /// <summary>
        /// Runs through the storage type (blob or file) to get the containers or shares that need to be copied.
        /// 
        /// Creates a unique name that is a mixture of the source storage account name and the container/share name such as
        ///     [SourceStorageAccountName]-[Original Container/Share name]
        ///     
        /// Creates the new location in the destination storage account, and finally creates the appropriate AzCopy command
        /// so that the source data can be copied to the new location in the destination account.
        /// </summary>
        /// <param name="sourceStorage">Source storage account</param>
        /// <param name="destinationStorage">Destination storage account</param>
        /// <param name="storageType">Blob or FileShare</param>
        /// <returns>List of AzCopy commands. </returns>
        private static List<String> PrepareStorageLocationCopy(AzureStorage sourceStorage, AzureStorage destinationStorage, StorageType storageType)
        {
            List<String> azCopyCommands = new List<string>();

            try
            {
                List<StorageLocation> stgLocations = sourceStorage.GetStorageLocations(storageType);
                foreach (StorageLocation location in stgLocations)
                {
                    // Get a unique name
                    String newLocationName = StorageLocationName.CreateLocationName(destinationStorage, storageType, sourceStorage.AccountName, location.Name);
                    // Set up the URL to the destination by replacing the account name and the new location name.
                    String destinationContainerUri = location.Uri.Replace(sourceStorage.AccountName, destinationStorage.AccountName);
                    destinationContainerUri = destinationContainerUri.Replace(location.Name, newLocationName);

                    if (destinationStorage.CreateStorageLocation(storageType, newLocationName))
                    {
                        azCopyCommands.Add(String.Format(STORAGE_COPY_COMMAND,
                            location.Uri,
                            destinationContainerUri,
                            sourceStorage.AccountKey,
                            destinationStorage.AccountKey
                            ));
                    }
                    else
                    {
                        Console.WriteLine(String.Format("Failed to create new location {0} of type {1}", newLocationName, storageType));
                    }
                }
            }
            catch(Exception ex)
            {
                if(storageType == StorageType.FileShare)
                {
                    Console.WriteLine(String.Format("The storage account {0} does not appear to support FileShares", sourceStorage.AccountName));
                }
                else
                {
                    throw ex;
                }
            }

            return azCopyCommands;
        }
     }
}
