package com.adf.spark;

import com.microsoft.azure.storage.CloudStorageAccount;
import com.microsoft.azure.storage.blob.CloudBlobClient;
import com.microsoft.azure.storage.blob.CloudBlobContainer;
import com.microsoft.azure.storage.blob.CloudBlockBlob;

import org.apache.commons.cli.*;
import org.apache.commons.io.FilenameUtils;
import org.apache.hadoop.conf.Configuration;
import org.apache.hadoop.conf.Configured;
import org.apache.hadoop.util.Tool;
import org.apache.hadoop.util.ToolRunner;

import org.apache.spark.launcher.SparkLauncher;

import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.util.ArrayList;
import java.util.List;
import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * This is a simple Map Reduce job that does not have a mapper or reducer. The main objective of this program is to
 * initiate the SparkLauncher so that we can submit a spark application. Since ADF currently support running Map Reduce
 * jobs on HDInsight but not Spark, we take the advantage of that to submit a Spark job through the Map Reduce job.
 * Then it will wait until the Spark job is finished and write any errors to the error stream.
 *
 * The jar file that contains the Spark application will be downloaded from a blob storage container. So make sure you copy
 * it to the blob storage before running this and provide the credentials and path in arguments as explained in the Read Me.
 *
 */
public class SparkJob extends Configured implements Tool {
    private static final Logger log = Logger.getLogger(SparkJob.class.getName());
    private static final String SPARK_HOME = "sparkHome";
    private static final String JAR_FILE = "jarFile";
    private static final String MAIN_CLASS = "mainClass";
    private static final String MASTER = "master";
    private static final String JARS = "jars";
    private static final String CONNECTION_STRING = "connectionString";
    private static final String DRIVER_MEMORY = "driverMemory";
    private static final String DRIVER_EXTRA_CLASSPATH = "driverExtraClasspath";
    private static final String EXECUTOR_CORES = "executorCores";
    private static final String EXECUTOR_MEMORY = "executorMemory";
    private static final String NUM_EXECUTORS = "numExecutors";
    private static final String EXTRA_FILES = "extraFiles";


    public static void main(String[] args) throws Exception {
        try {
            ToolRunner.run(new Configuration(), new SparkJob(), args);
        } catch (SparkException ex) {
            BufferedReader reader = null;
            try {
                reader = new BufferedReader(new InputStreamReader(ex.getErr()));
                String line = reader.readLine();
                while (line != null) {
                    System.err.println(line);
                    line = reader.readLine();
                }
            } finally {
                if (reader != null) {
                    reader.close();
                }
            }
            throw ex;
        }
    }

    public int run(String[] args) throws Exception {
        //handle the command line arguments
        Options options = new Options();
        CommandLineParser parser = new BasicParser();
        CommandLine cmd;

        options.addOption(SPARK_HOME, SPARK_HOME, true, "Spark Home Directory");
        options.addOption(JAR_FILE, JAR_FILE, true, "Spark Application Jar File");
        options.addOption(JARS, JARS, true, "Comma separated list of jars for the Spark Application");
        options.addOption(MAIN_CLASS, MAIN_CLASS, true, "Spark Application Main Class");
        options.addOption(MASTER, MASTER, true, "Master URL");
        options.addOption(DRIVER_MEMORY, DRIVER_MEMORY, true, "Spark Driver Memory");
        options.addOption(DRIVER_EXTRA_CLASSPATH, SparkLauncher.DRIVER_EXTRA_CLASSPATH, true, "Spark Driver Extra Classpath");
        options.addOption(EXECUTOR_CORES, EXECUTOR_CORES, true, "No of Executor Cores");
        options.addOption(EXECUTOR_MEMORY, EXECUTOR_MEMORY, true, "Spark Executor Memory");
        options.addOption(NUM_EXECUTORS, NUM_EXECUTORS, true, "Spark No of Executors");
        options.addOption(CONNECTION_STRING, CONNECTION_STRING, true, "Connection String for blob storage in which jar files contain");
        options.addOption(EXTRA_FILES, EXTRA_FILES, true, "Extra Files");

        try {
            cmd = parser.parse(options, args);
            validateArg(cmd, JAR_FILE);
            validateArg(cmd, SPARK_HOME);
            validateArg(cmd, MAIN_CLASS);
            validateArg(cmd, MASTER);
            validateArg(cmd, CONNECTION_STRING);
            validateArg(cmd, JARS);
        } catch (ParseException ex) {
            log.log(Level.SEVERE, "Error while parsing arguments", ex);
            throw new RuntimeException("Error while parsing arguments:" + ex.getMessage());
        }

        for(String s:cmd.getArgs()){
            System.out.println("===Args:"+s);
        }
        //invoke the Spark Submit
        SparkLauncher launcher = new SparkLauncher()
                .setSparkHome(cmd.getOptionValue(SPARK_HOME))
                .setAppResource(downloadFile(cmd.getOptionValue(CONNECTION_STRING), cmd.getOptionValue(JAR_FILE)))
                .setMainClass(cmd.getOptionValue(MAIN_CLASS))
                .setMaster(cmd.getOptionValue(MASTER))
                .setConf(SparkLauncher.DRIVER_MEMORY, cmd.getOptionValue(DRIVER_MEMORY, "2g"))
                .setConf(SparkLauncher.DRIVER_EXTRA_CLASSPATH, cmd.getOptionValue(DRIVER_EXTRA_CLASSPATH))
                .setConf(SparkLauncher.EXECUTOR_CORES, cmd.getOptionValue(EXECUTOR_CORES, "1"))
                .setConf(SparkLauncher.EXECUTOR_MEMORY, cmd.getOptionValue(EXECUTOR_MEMORY, "4g"))
                .addSparkArg("--num-executors", cmd.getOptionValue(NUM_EXECUTORS, "2"))
                .addAppArgs(filterArgs(cmd.getArgs()));

        String[] jars = getPaths(cmd.getOptionValue(JARS));
        for (String path : jars) {
            launcher.addJar(path);
        }
        
        //add extra files
        String[] extraFiles = getPaths(cmd.getOptionValue(EXTRA_FILES));
        for (String path : extraFiles) {
        	launcher.addFile(downloadFile(cmd.getOptionValue(CONNECTION_STRING), path));
        }
        
        Process spark = launcher.launch();

        //Wait for completion
        int result = spark.waitFor();
        if (result != 0) {
            throw new SparkException("Error while invoking Spark Job", result, spark.getErrorStream());
        }
        return result;
    }

    private String downloadFile(String connectionString, String path) {
        String destinationPath;
        try {
            String containerName = path.substring(0, path.indexOf("/"));
            String filePath = path.substring(path.indexOf("/") + 1);
            CloudStorageAccount storageAccount = CloudStorageAccount.parse(connectionString);
            CloudBlobClient blobClient = storageAccount.createCloudBlobClient();
            CloudBlobContainer container = blobClient.getContainerReference(containerName);
            CloudBlockBlob blob = container.getBlockBlobReference(filePath);
            destinationPath = "/tmp/" + FilenameUtils.getName(path);
            blob.downloadToFile(destinationPath);
        } catch (Exception e) {
            log.log(Level.SEVERE, "Error while downloading the spark jar file '" + path + "'", e);
            throw new RuntimeException("Error while downloading the spark jar file '" + path + "':" + e.getMessage());
        }
        return destinationPath;
    }

    private String[] getPaths(String path) {
        String[] paths = new String[0];
        if (path != null && !path.trim().isEmpty())
        	paths = path.split(",");
        return paths;
    }

    private void validateArg(CommandLine cli, String arg) {
        if (!cli.hasOption(arg)) {
            log.log(Level.SEVERE, "Missing required argument --" + arg);
            throw new RuntimeException("Missing required argument --" + arg);
        }
    }

    private static String[] filterArgs(String[]args){
        List<String> filteredArgs = new ArrayList<String>();
        for(String a:args){
                filteredArgs.add(a);
        }
       return filteredArgs.toArray(new String[]{});
    }
}
