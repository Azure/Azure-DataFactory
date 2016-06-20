package com.adf.jobonhdi;

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
 * submit any job so that we can submit any application on existing HDInsight cluster. Since ADF currently support running 
 * Map Reduce jobs on HDInsight, we take the advantage of that to submit a customized job through the Map Reduce job.
 * Then it will wait until the customized job is finished and write any errors to the error stream.
 *
 * The files, which are provided from property "--files", will be copied from blob storage account(s)
 * unless they already exist on cluster node. So make sure that you copy them to the blob storage before running this and 
 * you MAY need to provide the credential unless (the file is in format of "wasb://...") OR (the storage is linked to 
 * HDI cluster and it is used by property "jarLinkedService" in your adf pipeline).
 * 
 * Format of "wasb://..." is encouraged to make pipeline more secure.
 */
public class JobOnHdiLauncher
{
    private static final String FILES = "files";
    private static final String COMMAND = "command";

    // connection to FILES from azure storage blob
    private static final String CONNECTION_STRING = "connectionString";

    private boolean isWindowsOs;
    private CommandLine cmd;
    private List<String> linkedStorageAccounts;
    private List<CloudBlobClient> blobClients;
    
    public static void main(String[] args)
	{
    	JobOnHdiLauncher jobOnHdiLauncher = new JobOnHdiLauncher();
    	
    	try
    	{
    		jobOnHdiLauncher.parseCommandline(args);
    		jobOnHdiLauncher.createCloudBlobClients();
	    	int exitStatus = jobOnHdiLauncher.submitJob();   	
	        System.exit(exitStatus);		
    	}
    	catch (Exception ex)
    	{
            ex.printStackTrace();
            System.exit(-1);
    	}
	}
    
    public JobOnHdiLauncher()
    {
    	isWindowsOs = false;
    	linkedStorageAccounts = new LinkedList<String>();
    	blobClients = new LinkedList<CloudBlobClient>();
    }
    
    // submit the command that user provideds 
    public int submitJob() throws Exception
    {   	
        int exitStatus = 0;
        
    	String executionCmd = cmd.getOptionValue(COMMAND);
    	
    	// download files
    	if (cmd.hasOption(FILES))
    	{
    		downloadFiles(cmd.getOptionValue(FILES), this.isWindowsOs);       		
    	}
    	
        System.out.println("Start executing : " + executionCmd);
		
    	List<String> commands = new ArrayList<String>();
    	if (isWindowsOs)
    	{
    		commands.add("cmd");
        	commands.add("/c");
    	}
    	else
    	{
        	commands.add("/bin/sh");
            commands.add("-c");       		
    	}
        commands.add(executionCmd);
                
        ProcessBuilder pb = new ProcessBuilder(commands);
        pb.redirectError(ProcessBuilder.Redirect.INHERIT);
        pb.redirectOutput(ProcessBuilder.Redirect.INHERIT);
        Process cmdProcess = pb.start();
        exitStatus = cmdProcess.waitFor();

        if (exitStatus != 0)
        {
            System.err.println("Error while executing command : " + executionCmd);
        }
        else
        {
            System.out.println("Command succeeded!");
        }

        return exitStatus;
	}

    private void parseCommandline(String[] args)
    {
        Options options = new Options();
        CommandLineParser parser = new BasicParser();

        options.addOption(FILES, FILES, true, "Comma-separated list of files to be placed in the working directory");       
        options.addOption(COMMAND, COMMAND, true, "The command to be executed");
        options.addOption(CONNECTION_STRING, CONNECTION_STRING, true, "Connection String for blob storage in which FILES contain");

    	// For debugging purpose -- list environment
    	try
    	{
	    	java.net.InetAddress localMachine = java.net.InetAddress.getLocalHost();
	    	System.out.println("hostname : " + localMachine.getHostName());
    	}
    	catch (Exception ex)
    	{    		
    	}
    	System.out.println("user.name : " + System.getProperty("user.name"));
    	System.out.println("working dir : " + System.getProperty("user.dir"));
    	String osName = System.getProperty("os.name");
    	System.out.println("os.name : " + osName);
    	
    	this.isWindowsOs = osName.startsWith("Windows");
    	
    	// For debugging purpose -- list all arguments
    	for (String s : args)
    	{
            System.out.println("===Args 1 : " + s);
        }

        try 
        {     	
        	// skip first several args provided by ADF MapReduce
        	for (int i=0; i < args.length; i++)
        	{
        		if (args[i].startsWith("fs.azure.account.key"))
        		{
        			int keyStart = args[i].indexOf('=');
        			String accountName = args[i].substring("fs.azure.account.key.".length(), keyStart);
        			accountName = accountName.substring(0, accountName.indexOf('.'));
        			String accountKey = args[i].substring(keyStart+1);
        			
        			String account = "DefaultEndpointsProtocol=https;AccountName=" + accountName + ";AccountKey=" + accountKey;
        			linkedStorageAccounts.add(account);
        		}
        		
        		if (options.getOption(args[i]) != null)
        		{
                	args = Arrays.copyOfRange(args, i, args.length);
                	break;
        		}
        	}        		
        	
        	for (String s : args)
        	{
                System.out.println("===Args 2 : " + s);
            }

        	cmd = parser.parse(options, args);
            validateArg(COMMAND);
            
            if (cmd.hasOption(CONNECTION_STRING))
            {
            	linkedStorageAccounts.add(cmd.getOptionValue(CONNECTION_STRING));
            }
        } 
        catch (ParseException ex)
        {
        	System.err.println("Error while parsing arguments ...");
        	ex.printStackTrace();
            throw new RuntimeException("ParseException is thrown");
        }
    }
	
	private void validateArg(String arg)
	{
        if (!cmd.hasOption(arg))
        {
        	System.err.println("Missing required argument --" + arg);
            throw new RuntimeException("validateArg() failed.");
        }
    }

	private void createCloudBlobClients() throws Exception
	{
		Iterator<String> it = linkedStorageAccounts.iterator();
		
		while (it.hasNext())
		{
			String connectionString = null;
			try
			{
				connectionString = it.next();
				CloudStorageAccount storageAccount = CloudStorageAccount.parse(connectionString);
				blobClients.add(storageAccount.createCloudBlobClient());
			}
			catch (Exception ex)
			{
		       	System.err.println("Error while createCloudBlobClient '" + connectionString + "'");
		       	throw ex;		
			}
		}
	}
	
	private Set<String> downloadFiles(String filesToBeDownloaded, boolean isWindowsOs) throws Exception
	{
		return downloadFiles(filesToBeDownloaded, isWindowsOs, ",");
	}

	private Set<String> downloadFiles(String filesToBeDownloaded, boolean isWindowsOs, String delim) throws Exception
	{
        Set<String> filesDownloaded = new HashSet<String>();
    
    	String[] files = filesToBeDownloaded.split(delim);
    	
        for (String path : files)
    	{
        	path = path.trim();
        	
        	// Only copy Azure blob, skip copying from local worker nodes
        	if (path.startsWith("/"))
        	{
		        filesDownloaded.add(path);
	        	continue;       		
        	}
        	
        	if (path.toLowerCase().startsWith("wasb"))
        	{
                String fileName = path.substring(path.lastIndexOf("/") + 1);
                
		        if (!filesDownloaded.contains(fileName))
		        {
		        	HdfsCopy(path, fileName, isWindowsOs);
		        	filesDownloaded.add(fileName);
		        }

		        continue;       		
        	}

    		Iterator<CloudBlobClient> it = blobClients.iterator();
    		
        	int firstSlash = path.indexOf("/");
        	
        	if (firstSlash == -1)
        	{
                throw new RuntimeException("'" + path + "' is not a Azure storage blob in provided storage. Please use format container_name/blob_name.");
        	}
        	
	        String fileName = path.substring(firstSlash + 1);
	        String destFileName = path.substring(path.lastIndexOf("/") + 1);
	        
	        if (!filesDownloaded.contains(fileName))
	        {
	            String containerName = path.substring(0, firstSlash);
	            CloudBlobClient blobClient = null;
	            
	    		while (it.hasNext())
	    		{
		        	blobClient = it.next();

		            try
		            {
			            CloudBlobContainer container = blobClient.getContainerReference(containerName);
			            CloudBlockBlob blob = container.getBlockBlobReference(fileName);
			            blob.downloadToFile(destFileName);
				        filesDownloaded.add(destFileName);
				        System.out.println(path + " is copied successfully!");

				        break;
		            }
		            catch (Exception ex)
		            {
		            	// ignore Exception
			            blobClient = null;
		            }		            
	    		}
	    		
	    		// Download fails
	    		if (blobClient == null)
	    		{
	                throw new RuntimeException("Failed to download '" + path + "'. Please provide credential of storage account.");	        		
	    		}
	        }
    	}
        
        return filesDownloaded;
    }

	private static void HdfsCopy(String filePath, String destinationPath, boolean isWindowsOs) throws Exception
	{
        List<String> commands = new ArrayList<String>();
    	if (isWindowsOs)
    	{
    		commands.add("cmd");
        	commands.add("/c");
    	}
    	else
    	{
        	commands.add("/bin/sh");
            commands.add("-c");       		
    	}
        commands.add("hadoop fs -copyToLocal " + filePath + " " + destinationPath);
	
	    System.out.println("Start copying : " + commands.get(2));
	    
	    ProcessBuilder pb = new ProcessBuilder(commands);
	    Process copyProcess = pb.start();
	    int result = copyProcess.waitFor();
	
	    if (result != 0)
	    {
	        System.err.println("'" + filePath + "' is not a valid Azure storage blob. Please use format wasb://storage_url/container_name/blob_name.");
	        RedirectProcessResult(copyProcess.getErrorStream(), System.err);
	        throw new RuntimeException("Failed to copy " + filePath);
	    }
	    else
	    {
	        System.out.println(filePath + " is copied successfully!");
	        RedirectProcessResult(copyProcess.getInputStream(), System.out);
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
