# Image Classification
<sup>Created by Dan Grecoe, a Microsoft employee</sup>

This WPF based application is a simple image classification tool that can be used to manually classify full images for a deep learning scenario.

This is not an image tagging tool like [Microsoft VoTT](https://github.com/Microsoft/VoTT) or [labelimg](https://github.com/tzutalin/labelImg). 


##Sources
The tool currently supports 2 distinct sources of images:

1. Azure Blob Storage
2. Local Disk Files

### Azure Blob Storage
When using Azure Blob Storage, a catalog is created on disk where you specify it. It contains the links to the files in the storage account and not the image itself to save on disk space. 

When classifying images, the results are stored in a sub directory of the application directory - AzureStorageSourceCatalog.

The format of the files is CSV and can easily be imported by any data scientist when training a deep learning model. 

### Local Disk Files
When using Local Disk Files no catalog is needed as the directory structure is read in realtime. 

Like using Azure Blob Storage, the results are stored in a sub directory of the applicaiton directory - LocalStorageServiceCatalog.

The format of the files is CSV and can easily be imported by any data scientist when training a deep learning model. 


## General
To change the classificatons, on the configuration tab enter them in as a comma separated list and click Save Configuration. The Classification tab will update with the new values. 

Use the keyboard to quickly get through images:

    Ctrl + N -> Next Image
    Ctrl + P -> Previous Image
    Ctrl + 1 -> Check/Uncheck the first classification
    Ctrl + 2 -> Check/Uncheck the second classification
    etc....
    