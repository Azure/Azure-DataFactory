args=(commandArgs(TRUE))
if(length(args)==0){
    print("No arguments supplied.")
    ##supply default values
    output.plot = "/plot.png"
}else{
    for(i in 1:length(args)){
         eval(parse(text=args[[i]]))
    }
}

# Location of the data
bigDataDirRoot <- "/share"

# specify the input file in HDFS to analyze
inputFile <-file.path(bigDataDirRoot,"AirlineDemoSmall.csv")

#copy local file to HDFS
rxHadoopMakeDir(bigDataDirRoot)

if (!rxHadoopFileExists(inputFile)) {
 rxHadoopCopyFromLocal(system.file("SampleData/AirlineDemoSmall.csv",package="RevoScaleR"), bigDataDirRoot)
}

# define HDFS file system
myNameNode <- "default"
myPort <- 0
hdfsFS <- RxHdfsFileSystem(hostName=myNameNode, port=myPort)

# create Factors for days of the week
colInfo <- list(DayOfWeek = list(type = "factor",
                                 levels = c("Monday", "Tuesday", "Wednesday", "Thursday",
                                            "Friday", "Saturday", "Sunday")))

# define the data source
airDS <- RxTextData(file = inputFile, missingValueString = "M",
                    colInfo = colInfo, fileSystem = hdfsFS)

# First test the "local" compute context
rxSetComputeContext("local")

# Run a linear regression
system.time(
  model <- rxLinMod(ArrDelay~CRSDepTime+DayOfWeek, data = airDS)
)

# display a summary of model
summary(model)

# define MapReduce compute context
myHadoopMRCluster <- RxHadoopMR(consoleOutput=TRUE,
                                nameNode=myNameNode,
                                port=myPort,
                                hadoopSwitches="-libjars /etc/hadoop/conf")

# set compute context
rxSetComputeContext(myHadoopMRCluster)

# Run a linear regression
system.time(
  model1 <- rxLinMod(ArrDelay~CRSDepTime+DayOfWeek, data = airDS)
)

# display a summary of model
summary(model1)

# Run a linear regression
system.time(
  model <- rxLinMod(ArrDelay~F(CRSDepTime):F(DayOfWeek), data = airDS, cube = T)
)

# display a summary of model
summary(model)


linModDF <- model$countDF
sum(linModDF$ArrDelay - coef(model))


linModDF$coef.std.error <- as.vector(model$coef.std.error)
linModDF$lowerConfBound <- linModDF$ArrDelay - 2*linModDF$coef.std.error
linModDF$upperConfBound <- linModDF$ArrDelay + 2*linModDF$coef.std.error


linModDF$DepartureHour <- as.integer(levels(linModDF$F.CRSDepTime.))[linModDF$F.CRSDepTime.]

png("plot.png")
rxLinePlot( lowerConfBound + upperConfBound + ArrDelay ~ DepartureHour | F.DayOfWeek.,
            data = linModDF, lineColor = c("Blue1", "Blue2", "Red"),
            title = "Arrival Delay by Departure Hour: Weekdays and Weekends")

dev.off()
if (rxHadoopFileExists(output.plot)) {
	rxHadoopRemove(output.plot)
}
rxHadoopCopyFromLocal("plot.png", output.plot)




