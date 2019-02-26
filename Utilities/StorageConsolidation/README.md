# Storage Consolidation
<sup>Written by Dan Grecoe a Microsoft Employee</sup>


While cleaning up a group of Azure Subscriptions, I ran into hundreds of abandonded storage accounts. Now, these storage accounts held thousands of files. Some were code (that had not been saved in any other respository for a number of reasons) and some were data files that had either been collected or created for some data science project. 

The storage accounts couldn't be safely just deleted with the parent resource group because the data may be useful later, although the resource group did need to be deleted. 

That led me to creating this simple, but effective utility, that allows users to consolidate the data from across all of their storage accounts into a single storage account thereby allowing the deletion of those old and abandonded resource groups.

## Consolidating Files and Blobs
The consolidation will move all of the data from any number of source storage accounts to a destination storage account. When completed, the destination storage account will be populated with a series of new Azure Blob Containers and Azure File Shares. The names of these new locations will be in the form:

```
  [SourceStorageAccountName]-[Original Container/FileShare Name]
```
  - SourceStorageAccountName : The name of the source storage account, not the resource group or subscription it came from.
  - Original Container/FileShare Name : The original name of the sorce Blob Container or File Share

Further, if a container or file share exists with that name, the name will be appended with a '-N' where N is an integer value that is increased until a unique name in the storage account can be found.

## How it works
The application runs and determines what blob containers or file shares exist in the source storage accounts. Using the pattern described above, new blob containers or file shares are created in the destination storage account. 

During this process, the application will build a .bat file with the commands used in the [AzCopy](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-azcopy) utility. This Microsoft provided utility does the heavy lifting of cloud data copy operations, inlcuding rebuilding the destination location with the appropriate directories/etc. 

```
This application does not perform any destructive steps to any data in either the source or destination storage accounts. It simply creates new, uniquely named, Blob Containers or File Shares in the destination storage account so that the AzCpy tool can do the copy operation. It is up to the user to then remove the source storage accounts from their subscription.
```

## What you need to do....
To consolidate you will first need to do a few things:

1. Create a new storage account to be the destination storage account. This account will be used to hold all of the data from the other storage accounts that are to be removed.
2. Install [AzCopy](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-azcopy). 
3. Collect the connection strings for your new destination storage account and all of the source storage accounts. 
4. Download this code, modify the Configuration.json file with your storage account connection strings, then simply run the application. You will need Visual Studio to do so. This step will generate the appropriate locations in the destination storage account and create a .bat file that will do the actual copy actions.
5. Run the resulting .bat file to perform the actual copy of data from the source accounts to the destination account.
6. Optionally delete the original storage account and/or resource group.

## Configuring the application
As mentioned before, you will need to modify the Configuation.json file with the connection strings for the destination and source locations. The configuration file has the form:

```
{
  "Destination": "destination_storage_account_connection_string",
  "Sources": [
    "source_storage_account_connection_string_1",
    "source_storage_account_connection_string_2",
    "source_storage_account_connection_string_3"
  ]
}
```

__NOTE__: There can be any number of source storage accounts.

### Collecting Connection Strings
1. Visit the [Azure Portal](portal.azure.com)
2. Find the storage account you want the connection string of.
3. Click on the storage account to open the storage account blade.
4. Click on the menu item __Access keys__
5. Copy the value in __key1__/__Connection string__
6. Paste the value into the appropriate section of the Configuration.json file.