# A Developers Perspective: Building and deploying machine learning models using Microsoft Azure Machine Learning SDK with containers on a Kubernetes (AKS) cluster
<sup>Created by Dan Grecoe, a Microsoft employee</sup>


This repository introduces data science and machine learning at a very high level. Using that knowledge, a problem is identified at a manufacturing facility in which machine learning would be useful. 

Next, Azure Machine Learning SDK is introduced to create a machine learning model using Python and associated libraries. 

The model is then published using new Azure services to a Kubernetes cluster hosted in Azure as a container and operationalized as a REST endpoint. What this actually means is the model is placed behind a Flask app which is then exposed from the AKS cluster,

Lastly, an example on how to consume this model is provided as a C# console application. 

While manually following the steps is very helpful in understanding how all of the components come together, it is not critical. Reading the notebooks, in order, in this repository will give the reader a full understanding on what is going on.


**Prerequisites**

    - An Azure Subscription (provided)
    - A Windows laptop or, preferably, an Azure Windows Data Science Virtual Machine.       
        - The machine will need many python libraries including jupyter notebooks.
    - Instsall the Azure ML SDK onto the maachine you are working on.