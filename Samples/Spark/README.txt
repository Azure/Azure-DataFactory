New feature added:

1.  Added support to submit Spark Python application through ADF MapReduce job
2.  Added sample Python code and ADF pipeline for submitting Spark Python application
3.  Added support to run ADF without the need of providing credential of storage account
    Also added more sample pipelines with and without storage account connectionString. 
    If one of following conditions is met, no storage account connectionString is required
        a) The path of "--files", "--jars", "--py_files", "--properties_file" and "--appFile" is in format of "wasb://... or 
           already exist on worker nodes of cluster
        b) The path of files are from storage account which is linked to spark cluster (not the default storage account during
           spark cluster was created), and storage account is used by property "jarLinkedService" in your adf pipeline.

           In fact, this is the recommended way by ADF to set up yor spark cluster -- put all your files (binaries, dependencies, 
           input, output etc) in a storage account which is then linked to spark cluster.
4.  Now support following options of spark-submit
        --files
        --packages
        --jars
        --py-files
        --properties-file
        --deploy-mode
        --driver-cores
        --driver-memory
        --executor-cores
        --executor-memory
        --num-executors
        --class
    In addition, it also supports
        --appFile (This is required, it is .jar file for scala/java spark app, and .py file for spark python app)
        --app_arguments (any arguments for spark app)
        --connectionString
        --additional_options (which is like a wildcard option, so almost all spark-submit options can be supported by this,
                              for example, you can set the vaule of "--additional_options" to "--conf spark.driver.maxResultSize=2g")
5.  Added support to Hive query and operations on Hive table. Please include the following option in your adf
        "--files",
        "/usr/hdp/current/spark-client/conf/hive-site.xml",
6.  Support “--packages” option
7.  Added 5 scala samples with sbt configuration and Adf pipiline json to simulate different scenarios
        a. read from csv, filter out the header, define schema, create data frame, selected interested columns, save the result in blob with parquet format
        b. read data, save them into Hive table
        c. read data, insert them into existing Hive table        
        d. read data, process and save result into csv format by using package not included in spark-core by default
        e. pass arguments to spark app and process the arguments
8.  Redirect and save all errors/output (if there is any) from spark jobs into orignal ADF MapReduce job stderr/stdout for easy debugging.
    MapReduce and spark stderr/stdout log can be easily accessed from Spark cluster ambari UI, or ADF logs (from ADF UI or container "adfjobs"
    in Azure Storage Explorer).
9.  Fix a bug which MapReduce job was not completed immediately after Spark job was completed.
10. Make it more robust with differnt versions of cluster and various ADF properties 
11. Make it more robust to handle user "nobody" which the spark job is executed as on worker nodes


Usage:

CREATE a Azure blob container, for example "adflibs", and copy com.adf.sparklauncher.jar from folder \libs to adflibs. 
        If you also want to run sample ADF jobs, please also copy corrresponding binary too.

Set up DataSets and LinkedServices, sample of output dataset is at \ADFjsons\Datasets

Double check the version of your datanucleus*.jar on your cluster, and repleace your Pipilines with corresponding version for --jar argument, for example
    "--jars",
    "/usr/hdp/2.4.2.0-258/spark/lib/datanucleus-api-jdo-3.2.6.jar,/usr/hdp/2.4.2.0-258/spark/lib/datanucleus-rdbms-3.2.9.jar,/usr/hdp/2.4.2.0-258/spark/lib/datanucleus-core-3.2.10.jar",

    FYI -- ssh to your cluster, these datanucleus*.jar are located at /usr/hdp/<current_version>/spark/lib/ or /usr/hdp/<current_version>/hive/lib/. Most likely <current_version> is the right value.

Available arguments for "HDInsightMapReduce" activity:
        --files			// Comma-separated list of files to be placed in the working directory of each executor
        --packages		// Comma-separated list of maven coordinates of jars to include on the driver and executor classpaths
        --jars			// Comma-separated list of local jars to include on the driver and executor classpaths
        --py_files		// Comma-separated list of .zip, .egg, or .py files to place on the PYTHONPATH for Python apps
        --properties_file	// Path to a file from which to load extra properties
	--deploy_mode		// Where to launch the driver program, default is "cluster"
        --driver_cores		// Number of cores used by the driver, only in cluster mode
        --driver_memory		// Memory for driver (e.g. 1000M, 2G) 
        --executor_cores	// Number of cores per executor
        --executor_memory	// Memory per executor (e.g. 1000M, 2G) 
        --num_executors		// Number of executors to launch (Default: 2)
        --additional_options	// Additional options for spark-submit, 
                            	// for example --conf spark.driver.maxResultSize=2g, it is kind of like wildcard
*       --appFile		// Spark Application Jar or Python File
        --class	 		// Your application's main class (for Java / Scala apps only)
        --app_arguments		// Arguments for spark app
        --connectionString	// Connection String for blob storage in which Spark App (jar or python) file contain

Agrument with * (--appFile) means "required" argument.

Wayne Sun
