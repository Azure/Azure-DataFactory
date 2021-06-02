Customers have the ability to create a CustomLocation in Azure Data Factory.

CustomLocation is a generic location that can model tables that are not supported by out of box ADF tables. 
It exposes a property bag that can store key value pairs that can be shared by different custom activities. 

For example: The YahooFinanceMSFTTable is a custom Location that contains a property bag which can be shared by different custom activities.