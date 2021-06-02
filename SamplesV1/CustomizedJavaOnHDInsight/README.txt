This is a simple Map Reduce job that does not have a mapper or reducer. The main objective of this program is to
submit a general HDInsight job which runs on HDInsight cluster. Since ADF currently support running Map Reduce
jobs on HDInsight but not a general job, we take the advantage of that to submit a general job through the Map Reduce job.
Then it will wait until the job is finished and write any errors to the error stream.
 
This works for any VMs, both Linux and Windows, no additional library is needed, because of Java code vs c# code. It is also very flexible, 
you can run any executable with any arguments. No additional credential is needed if your binaries or any dependent files are put in blob storage
which is linked to HDInsight cluster.

You can specify any command you want to run through "--command" property. 
  "--files" property provides the files which are required to run your commands.
  All files which do not exist on HDInsight cluster will be downloaded automatically from a blob storage container.
  So make sure you copy them to the blob storage before running this.
  The credential for blob storage is not needed if the Azure storage already is linked to HDInsght cluster. 
  Otherwise, please provide the credential via "--connectionString" argument.
  You can put any commands (and arguments) in "--command" arguments.


Usage:

Create a Azure blob container, for example "adflibs", and copy com.adf.adfjobonhdi from folder \libs to adflibs. 
	If you also want to run sample ADF Jobs, please copy corrresponding binary too.

Set up DataSets and LinkedServices, sample of output dataset is at \ADFjsons\Datasets

Available arguments for "HDInsightMapReduce" activity:
        --files			// Comma-separated list of files to be placed in the working directory
*       --command		// The command (with any arguments if there is any) to execute your alication
        --connectionString	// Connection String for blob storage in which your App file and/or depended files are located

Agrument with * (--command) means "required" argument.

     Please note the "java" command behaves a little different on Windows and Linux. The files of parameter of "-cp" are seperated by ';' on windows and ':' on Linux.

Wayne Sun
