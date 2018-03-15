# A Developers Perspective: Building and deploying machine learning models using Microsoft Azure Machine Learning Workbench with containers on a Kubernetes cluster
<sup>Created by Dan Grecoe, a Microsoft employee</sup>


This repository introduces data science and machine learning at a very high level. Using that knowledge, a problem was is identified at a manufacturing facility in which machine learning would be useful. 

Next, Azure Machine Learning Workbench is introduced to create a machine learning model using Python and associated libraries. 

The model is then published using new Azure services to a Kubernetes cluster hosted in Azure as a container and operationalized as a REST endpoint.

Lastly, an example on how to consume this model is provided as a C# console application. 

While manually following the steps is very helpful in understanding how all of the components come together, it is not critical. Reading the document (docx) in this repository will give the reader a full understanding on what is going on.

If the reader does want to create all of the services provided they will need an Azure subscription that will support all of the services identified in the document. The fully developed solution will cost approximately $1,500 per month, or about $50 per day due to the virtual machines that are created.  

**Repository Content**


File/Directory | Purpose
--- | --- 
MlDsWorkbenchExample.docx | This document starts off by diving into Instructions to recreate the entire example from scratch
/Experiment | IPYNB files,Python files, and data used to create the experiment in Azure Machine Learning Workbench 
/Client | C# Source code (command line application) to call a published experiment
