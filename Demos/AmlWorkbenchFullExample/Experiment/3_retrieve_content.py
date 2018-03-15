# This file pulls the files out of the storage account (model and schema) and creates a directory with the needed files for deployment

#########################################################################
# Imports
#########################################################################
import os
from shutil import copyfile
from azure.storage.blob import BlockBlobService

#########################################################################
# Constants/file information
#########################################################################
AZURE_STORAGE_ACCOUNT_NAME = "<STORAGE_ACCOUNT_NAME>"
AZURE_STORAGE_ACCOUNT_KEY = "<STORAGE_ACCOUNT_KEY>"
AZURE_STORAGE_CONTAINER_NAME = "readydemo"
AZURE_STORAGE_BLOB_NAME = "factory.pkl"
AZURE_STORAGE_BLOB_NAME_SCHEMA = "factory.schema"

# This location will be unique to your system wherever you have set the project directory.
PROJECT_DIRECTORY = "<PATH_TO_EXPERIMENT_ON_DSVM>"

LOCAL_SYSTEM_PACKAGE_DIRECTORY = "{}/deploypackage".format(PROJECT_DIRECTORY)

CONDA_DEPENDENCIES = "{}/aml_config/conda_dependencies.yml".format(PROJECT_DIRECTORY)
SCORE_PYTHON = "{}/score.py".format(PROJECT_DIRECTORY)

MODEL_FILE_LOCAL = "{}/{}".format(LOCAL_SYSTEM_PACKAGE_DIRECTORY,AZURE_STORAGE_BLOB_NAME)
SCHEMA_FILE_LOCAL = "{}/{}".format(LOCAL_SYSTEM_PACKAGE_DIRECTORY,AZURE_STORAGE_BLOB_NAME_SCHEMA)
CONDA_FILE_LOCAL = "{}/conda_dependencies.yml".format(LOCAL_SYSTEM_PACKAGE_DIRECTORY)
SCORE_FILE_LOCAL = "{}/score.py".format(LOCAL_SYSTEM_PACKAGE_DIRECTORY)

#########################################################################
# Make sure package directory exists
#########################################################################
if not os.path.exists(LOCAL_SYSTEM_PACKAGE_DIRECTORY):
    os.makedirs(LOCAL_SYSTEM_PACKAGE_DIRECTORY)
    print('DONE creating a local directory!')
else:
    print('Local directory already exists!')

#########################################################################
# If files exist, remove them
#########################################################################
if os.path.isfile(MODEL_FILE_LOCAL):
    os.remove(MODEL_FILE_LOCAL)
    print("Local model file exists and was deleted")

if os.path.isfile(SCHEMA_FILE_LOCAL):
    os.remove(SCHEMA_FILE_LOCAL)
    print("Local schema file exists and was deleted")

if os.path.isfile(CONDA_FILE_LOCAL):
    os.remove(CONDA_FILE_LOCAL)
    print("Local conda dependencies file exists and was deleted")

if os.path.isfile(SCORE_FILE_LOCAL):
    os.remove(SCORE_FILE_LOCAL)
    print("Local python score file exists and was deleted")

#########################################################################
# Download files from storage account
#########################################################################
az_blob_service = BlockBlobService(account_name=AZURE_STORAGE_ACCOUNT_NAME, account_key=AZURE_STORAGE_ACCOUNT_KEY)
az_blob_service.get_blob_to_path(AZURE_STORAGE_CONTAINER_NAME, AZURE_STORAGE_BLOB_NAME, MODEL_FILE_LOCAL)
az_blob_service.get_blob_to_path(AZURE_STORAGE_CONTAINER_NAME, AZURE_STORAGE_BLOB_NAME_SCHEMA, SCHEMA_FILE_LOCAL)

#########################################################################
# Move other dependencies
#########################################################################
copyfile(CONDA_DEPENDENCIES,CONDA_FILE_LOCAL)
copyfile(SCORE_PYTHON,SCORE_FILE_LOCAL)

#########################################################################
# Create the deployment command, this assumes that you have a service 
# already set up and have the name to provide this output.
#########################################################################

# From https://docs.microsoft.com/en-us/azure/machine-learning/preview/model-management-configuration - Deploy Your Model
# az ml service create realtime --model-file [model file/folder path] -f [scoring file e.g. score.py] -n [your service name] -s [schema file e.g. service_schema.json] -r [runtime for the Docker container e.g. spark-py or python] -c [conda dependencies file for additional python packages]
print("az ml service create realtime --model-file {} -f {} -n {} -s {} -r spark-py -c {}"
.format(MODEL_FILE_LOCAL,SCORE_FILE_LOCAL,"[your service name]", SCHEMA_FILE_LOCAL,CONDA_FILE_LOCAL ))