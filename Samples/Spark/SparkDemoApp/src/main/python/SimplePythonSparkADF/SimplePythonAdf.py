from pyspark import SparkConf
from pyspark import SparkContext 
from pyspark.sql import SQLContext
from pyspark.sql.types import *

def main():
    sc = SparkContext()
    sqlContext = SQLContext(sc)

    # Read the csv data
    taxiFareRdd = sc.textFile("wasb://e2e@***.blob.core.windows.net/day_02.csv")

    # Create the schema
    taxiFareSchema = StructType([StructField("medallion", StringType(), False),StructField("hack_license", StringType(), False),StructField("vendor_id", StringType(), False),StructField("pickup_datetime", StringType(), False),StructField("payment_type", StringType(), False),StructField("fare_amount", FloatType(), False),StructField("surcharge", FloatType(), False),StructField("mta_tax", FloatType(), False),StructField("tip_amount", FloatType(), False),StructField("tolls_amount", FloatType(), False),StructField("total_amount", FloatType(), False)])

    # Parse the data
    taxiFareRdd2 = taxiFareRdd.map(lambda s: s.split(",")).filter(lambda s: s[0] != "medallion").map(lambda s:(str(s[0]), str(s[1]), str(s[2]), str(s[3]), str(s[4]), float(s[5]), float(s[6]), float(s[7]), float(s[8]), float(s[9]), float(s[10]) ))

    taxiFareDf = sqlContext.createDataFrame(taxiFareRdd2, taxiFareSchema)

    taxiFareDf2 = taxiFareDf.select("vendor_id", "pickup_datetime", "payment_type", "fare_amount", "tip_amount")
    taxiFareDf2.write.parquet("wasb://output@***.blob.core.windows.net/day_02")

if __name__ == "__main__":
    main()


