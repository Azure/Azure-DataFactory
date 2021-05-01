This script prints future scheduled triggers for your data factory. It gets pipeline  reference data and trigger list. For each trigger, it gets recurrence data. From recurrence data, it prints out all triggers scheduled from  current date and time for pipeline in reference.

To understand the script, please refer to Scheduled Trigger JSON definition sample as below. As you can see,  each trigger has typeProperties attribute that defines recurrence. The recurrence is key to getting future schedules for your pipeline.

```
{
    "properties": {
        "name": "MyTrigger",
        "type": "ScheduleTrigger",
        "typeProperties": {
            "recurrence": {
                "frequency": "Minute",
                "interval": 15,
                "startTime": "2017-12-08T00:00:00Z",
                "endTime": "2017-12-08T01:00:00Z",
                "timeZone": "UTC"
            }
        },
        "pipelines": [{
                "pipelineReference": {
                    "type": "PipelineReference",
                    "referenceName": "Adfv2QuickStartPipeline"
                },
                "parameters": {
                    "inputPath": "adftutorial/input",
                    "outputPath": "adftutorial/output"
                }
            }
        ]
    }
}
```

**Note:** It is a starter script. Please modify it to meet your needs. 
