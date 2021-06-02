Run Custom Map Reduce Using Azure Data Factory
Using Custom Map Reduce Activity in Azure Data Factory, we will run a Mahout Job that will help us determine the similarity between 2 items and calculate the ‘Item Similarity Matrix’. To know details about Mahout, use the following link 'http://mahout.apache.org/'. This sample essentially showcases that you can run a Map Reduce job (Custom Jar) on your HDInsight Cluster using Azure Data Factory.

MapReduceSample:
This sample contains the following:
1.Input Data:
This is the raw data that will be fed to the Mahout Job to calculate the Item Similarity Matrix.

2.Custom Jar File:
This is the Mahout Jar file that you want to run on HDInsight cluster using Azure Data Factory

3.Azure Data Factory Linked Services, Tables & Pipelines:
Azure Data Factory Linked Services, Tables, Pipelines to run the Custom Map Reduce. In this case, we are running a Mahout Jar and passing it various arguments to calculate the Item Similarity Matrix.

Preparing to Run the Sample:

a)Upload the ‘MahoutInputData.txt’ file inside ‘MapReduceSample\InputData’ folder to your storage account. Navigate to the Storage Account using ‘CloudXplorer’ or any other tool that you use to access your storage account. Create an ‘adfsamples’ container if it doesn’t exist already. Create a ‘Mahout\Input’ folder inside ‘adfsamples’ container. Upload ‘MahoutInputData.txt’ in ‘adfsamples\Mahout\Input’ folder.

b)Upload the mahout jar file inside ‘MapReduceSample\MahoutJar’ folder to your storage account. Navigate to the Storage Account using ‘CloudXplorer’ or any other tool that you use to access your storage account. Create an ‘adfsamples’ container if it doesn’t exist already. Create a ‘Mahout\Jars’ folder inside ‘adfsamples’ container. Upload mahout jar in ‘adfsamples\Mahout\Jars’ folder. The mahout jar is available in your HDInsight cluster and can be found here C:\apps\dist\mahout-0.9.0.2.2.7.1-34\examples\target\mahout-examples-0.9.0.2.2.7.1-34-job.jar. You need to upload this mahout jar to your storage account in "<container>/Mahout/Jars/" folder.