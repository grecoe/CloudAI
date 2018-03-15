# This script generates the scoring and schema files
# Creates the schema, and holds the init and run functions needed to 
# operationalize the Factory Classifcation sample

# Import data collection library. Only supported for docker mode.
# Functionality will be ignored when package isn't found
try:
    from azureml.datacollector import ModelDataCollector
    print("got the model collector")
except ImportError:
    print("Data collection is currently only supported in docker mode. May be disabled for local mode.")
    # Mocking out model data collector functionality
    class ModelDataCollector(object):
        def nop(*args, **kw): pass
        def __getattr__(self, _): return self.nop
        def __init__(self, *args, **kw): return None
    pass

import os

# Prepare the web service definition by authoring
# init() and run() functions. Test the functions
# before deploying the web service.
def init():
    global inputs_dc, prediction_dc, localFilePath
    import os
    from sklearn.externals import joblib
    from azure.storage.blob import BlockBlobService
    
    global AZURE_STORAGE_ACCOUNT_NAME
    global AZURE_STORAGE_ACCOUNT_KEY
    global AZURE_STORAGE_CONTAINER_NAME
    global AZURE_STORAGE_BLOB_NAME
    global AZURE_STORAGE_BLOB_NAME_SCHEMA
    global LOCAL_SYSTEM_DIRECTORY

	AZURE_STORAGE_ACCOUNT_NAME = "<STORAGE_ACCOUNT_NAME>"
	AZURE_STORAGE_ACCOUNT_KEY = "<STORAGE_ACCOUNT_KEY>"
	AZURE_STORAGE_CONTAINER_NAME = "readydemo"
    AZURE_STORAGE_BLOB_NAME = "factory.pkl"
    AZURE_STORAGE_BLOB_NAME_SCHEMA = "factory.schema"

    LOCAL_SYSTEM_DIRECTORY = "modelfile"

    localFilePath = "./{}/{}".format(LOCAL_SYSTEM_DIRECTORY,AZURE_STORAGE_BLOB_NAME)

    # Download the model
    if not os.path.exists(LOCAL_SYSTEM_DIRECTORY):
        os.makedirs(LOCAL_SYSTEM_DIRECTORY)
    
    if os.path.isfile(localFilePath):
        os.remove(localFilePath)
    
    az_blob_service = BlockBlobService(account_name=AZURE_STORAGE_ACCOUNT_NAME, account_key=AZURE_STORAGE_ACCOUNT_KEY)
    az_blob_service.get_blob_to_path(AZURE_STORAGE_CONTAINER_NAME, AZURE_STORAGE_BLOB_NAME, localFilePath)

    # load the model file
    global model
    model = joblib.load(localFilePath)

    inputs_dc = ModelDataCollector(localFilePath, identifier="inputs")
    prediction_dc = ModelDataCollector(localFilePath, identifier="prediction")

def upload_schema(localfile):
    from azure.storage.blob import BlockBlobService
    from azure.storage.blob import ContentSettings
    
    az_blob_service = BlockBlobService(account_name=AZURE_STORAGE_ACCOUNT_NAME, account_key=AZURE_STORAGE_ACCOUNT_KEY)
    az_blob_service.create_blob_from_path(
        AZURE_STORAGE_CONTAINER_NAME,
        AZURE_STORAGE_BLOB_NAME_SCHEMA,
        localfile,
        content_settings=ContentSettings(content_type='application/json'))

def run(input_df):
    import json
    pred = model.predict(input_df)
    return json.dumps(str(pred))


def main():
  from azureml.api.schema.dataTypes import DataTypes
  from azureml.api.schema.sampleDefinition import SampleDefinition
  from azureml.api.realtime.services import generate_schema
  import pandas
  
  # temp=45.9842594460449, volt=150.513223075022, rotate=277.294013981084, state=0.0, time=1.0, id=1.0
  df = pandas.DataFrame(data=[[45.9842594460449, 150.513223075022, 277.294013981084, 1.0, 1.0], [46.9842594460449, 152.513223075022, 277.294013981084, 2.0, 1.0]], columns=['temp', 'volt','rotate','time', 'id'])

  print(df.dtypes)

  # Turn on data collection debug mode to view output in stdout
  os.environ["AML_MODEL_DC_DEBUG"] = 'true'

  # Test the output of the functions
  init()
  
  print("past init building inputs?")

  inputs = {"input_df": SampleDefinition(DataTypes.PANDAS, df)}

  print("calling run?")
  res = run(df)
  print(res)

  #Genereate the schema
  generate_schema(run_func=run, inputs=inputs, filepath='./outputs/service_schema.json')
  print("Schema generated")
  upload_schema('./outputs/service_schema.json')
  print("Schema uploaded")

if __name__ == "__main__":
    main()

