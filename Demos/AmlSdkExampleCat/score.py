import os
import pickle
import json
import pandas as pd
from sklearn.externals import joblib
from azureml.core.model import Model

# Prepare the web service definition by authoring
# init() and run() functions. Test the functions
# before deploying the web service.
def init():
    # load the model file
    global model
    model_path = Model.get_model_path(model_name = 'factorymodel.pkl')
    print("Model Path Is: {}".format(model_path))
    model = joblib.load(model_path)
    print(model)


def run(input_df):
    try:
        # data = json.loads(raw_data)['data']
        # data = numpy.array(data)
        # result = model.predict(data)
        # return json.dumps({"result": result.tolist()})
        data = json.loads(input_df)['input_df']
        data = pd.DataFrame(data)
        pred = model.predict(data)
        return json.dumps(str(pred))
    except Exception as e:
        result = str(e)
        return json.dumps({"INTERNAL error": result})

def main():
    init()
    
    test_sample = json.dumps({'input_df':[{'id':1.0,'volt':241.0,'rotate':120.0,'temp':189.0,'time':3.0}]})
    result = run(test_sample)
    print("result {}".format(result))


if __name__ == "__main__":
    main()
