Customers have the ability to create a CustomLinkedService in Azure Data Factory.

CustomLinkedService is a generic linked service that can be used by custom activity to define sources that are not supported out of the box by ADF. 
It exposes a property bag that can store key value pairs that can be shared by different tables.

For example: The YahooFinanceLinkedService is a custom Linked Service that has an http endpoint as a property bag. Now this Linked Service can be shared across
multiple tables.