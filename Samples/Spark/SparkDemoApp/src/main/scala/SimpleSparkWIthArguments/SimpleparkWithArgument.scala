import org.apache.spark.SparkConf
import org.apache.spark.SparkContext
import org.apache.spark.SparkContext._

object SimpleparkWithArgument
{
    def main(args: Array[String]) 
    {
        val conf = new SparkConf().setAppName("Simple Spark With Arguments")
        val sc = new SparkContext(conf)

        print("Hi");

        for ( x <- args )
        {
             print(" " + x);
        }

        println("!");
    }
}