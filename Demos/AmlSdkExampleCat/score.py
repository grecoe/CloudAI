import os
import pickle
import json
from sklearn.externals import joblib
from azureml.assets.persistence.persistence import get_model_path

# Prepare the web service definition by authoring
# init() and run() functions. Test the functions
# before deploying the web service.
def init():
    # load the model file
    global model
    model_path = get_model_path(model_name = 'factorymodel.pkl')
    #model_path = './factorymodel.pkl'
    print("Model Path Is: {}".format(model_path))
    model = joblib.load(model_path)
    print(model)


def run(input_df):
    try:
        # data = json.loads(raw_data)['data']
        # data = numpy.array(data)
        # result = model.predict(data)
        # return json.dumps({"result": result.tolist()})
        print("Getting Prediction")
        pred = model.predict(input_df)
        print(str(pred))
        return json.dumps(str(pred))
    except Exception as e:
        result = str(e)
        return json.dumps({"INTERNAL error": result})

def main():
    init()

if __name__ == "__main__":
    main()
