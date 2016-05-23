package com.adf.sparklauncher;

import java.io.*;
import java.util.*;

import org.apache.commons.cli.BasicParser;
import org.apache.commons.cli.CommandLine;
import org.apache.commons.cli.CommandLineParser;
import org.apache.commons.cli.Options;
import org.apache.commons.cli.ParseException;

import com.microsoft.azure.storage.CloudStorageAccount;
import com.microsoft.azure.storage.blob.CloudBlobClient;
import com.microsoft.azure.storage.blob.CloudBlobContainer;
import com.microsoft.azure.storage.blob.CloudBlockBlob;

/**
 * This is a simple Map Reduce job that does not have a mapper or reducer. The main objective of this program is to
 * submit Spark job so that we can submit a spark application. Since ADF currently support running Map Reduce
 * jobs on HDInsight but not Spark, we take the advantage of that to submit a Spark job through the Map Reduce job.
 * Then it will wait until the Spark job is finished and write any errors to the error stream.
 *
 * The jar file that contains the Spark application will be downloaded from a blob storage container. So make sure you copy
 * it to the blob storage before running this and provide the credentials and path in arguments.
 */
public class AdfSparkJobLauncher
{
	// spark-submit options for this specific Spark Application
    private static final String FILES = "files";
    private static final String PACKAGES = "packages";
    private static final String JARS = "jars";

    // additional options for spark-submit, for example
    // --conf spark.driver.maxResultSize=2g
    private static final String ADDITIONAL_OPTION = "additional_options";

    // yarn and spark settings
    private static final String DEPLOY_MODE = "deploy_mode"; // default "cluster"
    private static final String DRIVER_CORES = "driver_cores";
    private static final String DRIVER_MEMORY = "driver_memory";
    private static final String EXECUTOR_CORES = "executor_cores";
    private static final String EXECUTOR_MEMORY = "executor_memory";
    private static final String NUM_EXECUTORS = "num_executors";
    
    // spark application
    private static final String APP_FILE = "appFile"; // Spark Application Jar or Python File
    private static final String MAIN_CLASS = "class"; // Your application's main class (for Java / Scala apps only)
    private static final String APP_ARGUMENTS = "app_arguments";

    // connection to load APP_FILE from azure storage blob
    private static final String CONNECTION_STRING = "connectionString";

    public static void main(String[] args)
	{
        Options options = new Options();
        CommandLineParser parser = new BasicParser();
        CommandLine cmd = null;

        options.addOption(FILES, FILES, true, "Comma-separated list of files to be placed in the working directory of each executor");
        options.addOption(PACKAGES, PACKAGES, true, "Comma-separated list of maven coordinates of jars to include on the driver and executor classpaths");
        options.addOption(JARS, JARS, true, "Comma-separated list of local jars to include on the driver and executor classpaths");

        options.addOption(ADDITIONAL_OPTION, ADDITIONAL_OPTION, true, "Additional options for spark-submit");
        
        options.addOption(DEPLOY_MODE, DEPLOY_MODE, true, "Where to launch the driver program");
        options.addOption(DRIVER_CORES, DRIVER_CORES, true, "Number of cores used by the driver");
        options.addOption(DRIVER_MEMORY, DRIVER_MEMORY, true, "Spark Driver Memory");
        options.addOption(EXECUTOR_CORES, EXECUTOR_CORES, true, "No of Executor Cores");
        options.addOption(EXECUTOR_MEMORY, EXECUTOR_MEMORY, true, "Spark Executor Memory");
        options.addOption(NUM_EXECUTORS, NUM_EXECUTORS, true, "Spark No of Executors");
        
        options.addOption(APP_FILE, APP_FILE, true, "Spark Application Jar or Python File");
        options.addOption(MAIN_CLASS, MAIN_CLASS, true, "Your Spark Application's main class (for Java / Scala apps)");
        options.addOption(APP_ARGUMENTS, APP_ARGUMENTS, true, "The srguments for Spark Application");

        options.addOption(CONNECTION_STRING, CONNECTION_STRING, true, "Connection String for blob storage in which jar files contain");

        // For debugging purpose -- list all arguments
    	for (String s : args)
    	{
            System.out.println("===Args 1 : " + s);
        }

        try 
        {
        	// skip first 6 args provided by ADF MapReduce
        	args = Arrays.copyOfRange(args, 6, args.length);
        	
            cmd = parser.parse(options, args);
            validateArg(cmd, APP_FILE);
            validateArg(cmd, CONNECTION_STRING);
        } 
        catch (ParseException ex)
        {
        	System.err.println("Error while parsing arguments ...");
        	ex.printStackTrace();
        }
        
        // Run "spark-submit" with given arguments 
        try
		{
        	String appFile = downloadFile(cmd.getOptionValue(CONNECTION_STRING), cmd.getOptionValue(APP_FILE));
        	
        	StringBuilder sparkSubmitCmd = new StringBuilder();
        	sparkSubmitCmd.append("spark-submit");
        	sparkSubmitCmd.append(" --master yarn --deploy-mode " + cmd.getOptionValue(DEPLOY_MODE, "cluster"));

        	if (cmd.hasOption(PACKAGES))
        	{
            	sparkSubmitCmd.append(" --conf spark.jars.ivy=/tmp/.ivy2");
            	sparkSubmitCmd.append(" --packages " + cmd.getOptionValue(PACKAGES));       		
        	}      	
        	if (cmd.hasOption(ADDITIONAL_OPTION))
        	{
        		sparkSubmitCmd.append(" " + cmd.getOptionValue(ADDITIONAL_OPTION));
        	}
        	if (cmd.hasOption(FILES))
        	{
            	sparkSubmitCmd.append(" --files " + cmd.getOptionValue(FILES));       		
        	}
        	if (cmd.hasOption(JARS))
        	{
        		sparkSubmitCmd.append(" --jars " + cmd.getOptionValue(JARS));
        	}
        	if (cmd.hasOption(DRIVER_CORES))
        	{
        		sparkSubmitCmd.append(" --driver-cores " + cmd.getOptionValue(DRIVER_CORES));
        	}
        	if (cmd.hasOption(DRIVER_MEMORY))
        	{
        		sparkSubmitCmd.append(" --driver-memory " + cmd.getOptionValue(DRIVER_MEMORY));
        	}
        	if (cmd.hasOption(EXECUTOR_CORES))
        	{
        		sparkSubmitCmd.append(" --executor-cores " + cmd.getOptionValue(EXECUTOR_CORES));
        	}
        	if (cmd.hasOption(EXECUTOR_MEMORY))
        	{
        		sparkSubmitCmd.append(" --executor-memory " + cmd.getOptionValue(EXECUTOR_MEMORY));
        	}       	
        	sparkSubmitCmd.append(" --num-executors " + cmd.getOptionValue(NUM_EXECUTORS, "2"));
        	
        	// Application's main class (for Java / Scala apps only)
        	// "--class" is not required by spark-submit
        	if (appFile.endsWith(".jar") && cmd.hasOption(MAIN_CLASS))
        	{
            	sparkSubmitCmd.append(" --class " + cmd.getOptionValue(MAIN_CLASS));
        	}       	
        	sparkSubmitCmd.append(" " + appFile);
        	if (cmd.hasOption(APP_ARGUMENTS))
        	{
        		sparkSubmitCmd.append(" " + cmd.getOptionValue(APP_ARGUMENTS));
        	}       	

            System.out.println("Submit Spark Job: " + sparkSubmitCmd.toString());
			
        	List<String> commands = new ArrayList<String>();
        	commands.add("/bin/sh");
            commands.add("-c");
            commands.add(sparkSubmitCmd.toString());
            
            ProcessBuilder pb = new ProcessBuilder(commands);
            Process sparkProcess = pb.start();
            int result = sparkProcess.waitFor();

	        if (result != 0)
	        {
	            System.err.println("Error while invoking Spark Job: ");
	            RedirectProcessResult(sparkProcess.getErrorStream(), System.err);
	        }
	        else
	        {
	            System.out.println("Spark Job succeeded!");
	            RedirectProcessResult(sparkProcess.getInputStream(), System.out);
	        }
		}
		catch (IOException ex)
		{
            System.err.println("IOException happened - ");
            ex.printStackTrace();
            System.exit(-1);
        }
		catch (InterruptedException ex)
		{
            System.err.println("InterruptedException happened - ");
            ex.printStackTrace();
            System.exit(-1);		
		}

        System.exit(0);		
	}
	
	private static String downloadFile(String connectionString, String path)
	{
        String destinationPath = "";
    
        try
        {
            String containerName = path.substring(0, path.indexOf("/"));
            String filePath = path.substring(path.indexOf("/") + 1);
            CloudStorageAccount storageAccount = CloudStorageAccount.parse(connectionString);
            CloudBlobClient blobClient = storageAccount.createCloudBlobClient();
            CloudBlobContainer container = blobClient.getContainerReference(containerName);
            CloudBlockBlob blob = container.getBlockBlobReference(filePath);
            destinationPath = "/tmp/" + filePath;
            blob.downloadToFile(destinationPath);
        }
        catch (Exception ex)
        {
        	System.err.println("Error while downloading the spark jar file '" + path + "'");
        	ex.printStackTrace();
        }
        
        return destinationPath;
    }
	
	private static void validateArg(CommandLine cli, String arg)
	{
        if (!cli.hasOption(arg))
        {
        	System.err.println("Missing required argument --" + arg);
            throw new RuntimeException("Missing required argument --" + arg);
        }
    }
	
	private static void RedirectProcessResult(InputStream inStream, PrintStream printStream) throws IOException
	{
        BufferedReader streamReader = null;
        String line = null;

        try
        {
        	streamReader = new BufferedReader(new InputStreamReader(inStream));
            while ((line = streamReader.readLine()) != null)
            {
            	printStream.println(line);
            }
        }
        finally
        {
            if (streamReader != null)
            {
            	streamReader.close();
            }
        }
	}
}
