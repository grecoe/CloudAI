# Demos - Cognitive Services
<sup>Created by Dan Grecoe, a Microsoft employee</sup>

This demo is a series of Azure Services stitched together to process digital articles that contain media (images and videos) in an automated fashion.

RSSGenerator is used to populate a Cosmos DB Collection with RSS articles in multiple languages and the Azure Function App (wwwroot) contains the functions that:

1. ArticlesIngestTrigger function triggered from a CosmosDB insert, breaks apart items marked as articles into a separate queue for translation.
2. The TranslationQueueFunction function is triggered from the first queue. This detects the language of the incoming text (title and body) and then, if not in the predetermined language, translates it to the desired language. Next it processes that text to find key phrases, sentiment value, and entities. This function then passes along the id of the Cosmos Document for the next function.
3. The OCRQueueTrigger is next in line. It looks at images and (1) pulls the text from the image and using OCR and (2) uses computer vision to determine what is in the image.
4. The FaceAPIQueueTrigger function looks again through the images and uses the Face API service to determine (1) number of people (2) gender and age of those found.
5. The InspectionQueueTrigger is the last funciton that can be used to go through all of the processed information from an article and determine if a notification needs to be sent somewhere. This notification work has NOT been coded and is left up to the reader.

This is all done with out of the box Microsoft Azure Services. The docx in this directory provides much more detail to what is going on.
