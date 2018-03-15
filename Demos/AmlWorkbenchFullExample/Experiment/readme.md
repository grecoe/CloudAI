# Microsoft Ready CD-ARC321

This package contains the IPython Notebooks and Python scripts neccesary for creating the example shownn during the talk on February 2, 2018 at Ready in Seattle.

In this github repository is the CD-ARC321.doc file that gives detailed instructions on how to create the neccesary resources and instructions for creating the example given.

# Contents

    1_buildmodel.ipynb*
        Notebook that creates a model and uploads it to Azure storage.
    2_modelschema.ipynb*
        Create the schema for the model and uploads it to Azure Storage.
    3_retrieve_content.py
        Collects all of the files neccesary for creating a service.
    score.py*
        Python file that sits behind the service.
    data/dataset.csv
        Dataset used for creating the model

*Requires modifications for the Azure Storage Account created for the demo