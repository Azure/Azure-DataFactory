import org.apache.spark.SparkConf
import org.apache.spark.SparkContext
import org.apache.spark.SparkContext._
import org.apache.spark.sql.types._
import org.apache.spark.sql._
import org.apache.spark.sql.hive.HiveContext

// Create Hive table from csv data
object AdfHiveTable1
{
    def main(args: Array[String]) 
    {
        val conf = new SparkConf().setAppName("Adf Hive Table1")
        val sc = new SparkContext(conf)
        val sqlContext = new HiveContext(sc)

        val taxiFareRdd = sc.textFile("wasb://e2e@***.blob.core.windows.net/day_01.csv")
        val header = taxiFareRdd.first()
        val taxiFareRdd2 = taxiFareRdd.filter(x => x != header)

        val taxiFareSchema = StructType(List(StructField("medallion", StringType, false), StructField("hack_license", StringType, false), StructField("vendor_id", StringType, false), StructField("pickup_datetime", StringType, false), StructField("payment_type", StringType, false), StructField("fare_amount", FloatType, false), StructField("surcharge", FloatType, false), StructField("mta_tax", FloatType, false), StructField("tip_amount", FloatType, false), StructField("tolls_amount", FloatType, false), StructField("total_amount", FloatType, false) ))

        val taxiFareRdd3 = taxiFareRdd2.map((s) => s.split(",")).map((s) => Row(s(0), s(1), s(2), s(3), s(4), s(5).toFloat, s(6).toFloat, s(7).toFloat, s(8).toFloat, s(9).toFloat, s(10).toFloat))

        val taxiFareDf = sqlContext.createDataFrame(taxiFareRdd3, taxiFareSchema)
        taxiFareDf.saveAsTable("AdfFareTable")
    }
}