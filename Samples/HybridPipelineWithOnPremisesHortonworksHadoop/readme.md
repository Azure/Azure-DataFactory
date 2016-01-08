# On-premise and cloud hybrid Hadoop data pipelines with Hortonworks and Cortana Analytics #

[Azure Data Factory](https://azure.microsoft.com/en-us/services/data-factory/) and [Hortonworks Falcon team](http://hortonworks.com/hadoop/falcon/) jointly announced the availability of private preview for building hybrid Hadoop data pipelines leveraging on-premises Hortonworks Hadoop clusters and cloud based Cortana Analytics services like HDInsight Hadoop clusters and Azure Machine Learning.

Customers maintaining on-premises Hadoop based data lakes need to often times enable hybrid data flows extending on-premises data lake into the cloud for various reasons:

1. Keep **PII and other sensitive data** on-premises for privacy, compliance reasons but leverage cloud for elastic scale for workloads that don’t need the sensitive information.
2. Leverage cloud for **cross region replication, disaster recovery at scale**.
3. Leverage cloud for **dev, test environments**.

Hybrid Hadoop pipeline preview enables these scenarios by allowing you to now add your on-premises Hadoop cluster as a compute target for running jobs in Data Factory just like you would add other compute targets like an HDInsight based Hadoop cluster in cloud. 

![Hybrid Pipelines Overview](./DocumentationImages/hybridpipeline.jpg)

You can enable connectivity between your on-premises cluster with data factory service over a secure channel with just a few clicks. Once you do that as shown above you can develop a hybrid pipeline that does the following:

1.	Run Hadoop Hive & Pig jobs on-premises with the new on-premises Hadoop Hive, Pig activities in data factory.
2.	Copy data from on-premises HDFS to Azure blob in cloud with the new on-premises replication activity.
3.	Add more steps to the pipeline and continue big data processing in cloud with Hadoop HDInsight activity for example.

The private preview is available for a small set of customers participating in Hortonworks Dec 2015 tech preview (TODO: add link to tech preview).
 
For more details on how hybrid pipelines are enabled under the covers, how data factory and Falcon communicate with each other and step by step instructions on how to set this up if you are part of the private preview please refer to our GitHub sample for hybrid Hadoop pipelines.
