New feature added:

1. Added support to submit Spark Python application through ADF MapReduce job
2. Added sample Python code and ADF pipeline for submitting Spark Python application
3. Added following more options of spark-submit
	--files
	--packages
	--driver-cores
	--app_arguments
	--additional_options (which is like a wildcard argument, so almost all spark-submit options can be supported by this)
4. Added support to schedule a "real" spark job, for example reading from blobs and running sql queries etc. 
   The main challenge is to make Hive table visible to Power BI and Tableau
5. Made “--packages” option really work
   The challenge is "HDInsightMapReduce" job in ADF actually executes spark job as user “nobody” which does not have home directory, however packages require to be downloaded to all worker nodes
6. Added 5 scala samples with sbt configuration and Adf pipiline json to simulate different scenarios
	a. read from csv, filter out the header, define schema, create data frame, selected interested columns, save the result in blob with parquet format
	b. read data, save them into Hive table
	c. read data, insert them into existing Hive table
	d. read data, process and save result into csv format by using package not included in spark-core by default
	c. pass arguments to spark app and process the arguments
7. Redirect and save all errors from spark jobs (if there is any) into orignal ADF MapReduce job status/stderr for better debugging. Redirect and save stdout of spark job in the log of Spark Application, which can be easily accessed from Spark cluster UI.


Usage:

CREATE a Azure blob container, for example "adflibs", and copy com.adf.sparklauncher.jar from folder \\scratch2\scratch\waynesun\AdfSpark\libs to adflibs. 
	If you also want to run sample ADF Jobs, please copy corrresponding binary too.

Set up DataSets and LinkedServices, same as https://github.com/Azure/Azure-DataFactory/tree/master/Samples/Spark/Spark-ADF/src/ADFjsons

Double check the version of your datanucleus*.jar on your cluster, and repleace your Pipilines with corresponding version for --jar argument
    "--jars",
    "/usr/hdp/2.4.2.0-258/spark/lib/datanucleus-api-jdo-3.2.6.jar,/usr/hdp/2.4.2.0-258/spark/lib/datanucleus-rdbms-3.2.9.jar,/usr/hdp/2.4.2.0-258/spark/lib/datanucleus-core-3.2.10.jar",

    FYI -- ssh to your cluster, these datanucleus*.jar are located at /usr/hdp/<current_version>/spark/lib/ or /usr/hdp/<current_version>/hive/lib/. Most likely <current_version> is the right version.

Available arguments for "HDInsightMapReduce" activity:
        --files			// Comma-separated list of files to be placed in the working directory of each executor
        --packages		// Comma-separated list of maven coordinates of jars to include on the driver and executor classpaths
        --jars			// Comma-separated list of local jars to include on the driver and executor classpaths
        --additional_options	// Additional options for spark-submit, for example --conf spark.driver.maxResultSize=2g, it is kind of like wildcard.
	--deploy_mode		// default "cluster"
        --driver_cores		// Number of cores used by the driver, only in cluster mode
        --driver_memory		//  Memory for driver (e.g. 1000M, 2G) 
        --executor_cores	// Number of cores per executor
        --executor_memory	// Memory per executor (e.g. 1000M, 2G) 
        --num_executors		// Number of executors to launch (Default: 2)
*       --appFile		// Spark Application Jar or Python File
        --class	 		// Your application's main class (for Java / Scala apps only)
        --app_arguments		// arguments for spark app
*       --connectionString	// Connection String for blob storage in which Spark App (jar or python) file contain

Agrument with * (--appFile, --connectionString) means "required" arguments.

Wayne Sun
