# Running RevoScaleR job on HDInsight

## About

This example shows how to run an R Script that uses RevoScaleR to run distributed algorithms on BigData using Hadoop.

It should be executed on a LinkedService of type "R Server on HDInsight" (also called HDInsight Premium). This HDInsight cluster has Microsoft R Server installed on each node.

## Explanation of the command line

The RevoScaleR script should be uploaded to the Azure Storage container linked to the HDInsight cluster. In this example, it is uploaded in the /example/rdata/ folder.

The first argument to the JobOnHdiLauncher class copies an entire directory from Azure Storage, containing the R script (NB: trailing slash is important):

```
"--files", "wasbs:///example/rdata/"
```
The second argument specifies the command to run:
```
 "--command", "env -i R CMD BATCH --no-save --no-restore \"--args output.plot='/plot.png'\" rdata/airlineDelays.r /dev/stdout"
```

This is a pretty complicated command line, so we explain it below:
| Command                            | Explanation                                                                                                                              |
|------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------|
| env -i                             | Run the command with an empty environment. This prevents Hadoop or classpath environment variables from affecting RevoScaleR.            |
| R                                  | Run R interpreter at /usr/bin/R                                                                                                          |
| CMD BATCH                          | Run interpreter in batch mode                                                                                                            |
| --no-save --no-restore             | Do not save and restore workspace (saves time)                                                                                           |
| \"--args output.plot='/plot.png'\" | Optionally, provide arguments to the R script. In this example the script uploads the plot to Azure Storage                            |
| rdata/airlineDelays.r                    | The R script to invoke. Note the rdata folder was copied from Azure Storage using the --files option.                            |
| /dev/stdout                        | Direct R output to standard output, so that it can be downloaded from Data Factory portal (otherwise would be written in airlineDelays.r.Rout) |

It is also possible to put the command line in a shell script file instead.

