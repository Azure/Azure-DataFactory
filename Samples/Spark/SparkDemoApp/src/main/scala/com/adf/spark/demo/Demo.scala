package com.adf.spark.demo

import org.apache.spark.{SparkConf, SparkContext}

/**
 * This sample Spark App reads  text file(s) such as CSV files from 'input' location and
 * writes them back to the given output location. Effectively it is a Copy operation.
 * It expects two named arguments separated by space in the following format. The paths can be your blob storage paths.
 * Please note that the input path can be a path to a directory where you have more than one file.
 * input=<your input path> output=<your output path>
 *
 *
 * You can test this on your Spark cluster independent of ADF using spark-submit
 *
 * spark-submit --master yarn-client sparkdemoapp_2.10-1.0.jar com.adf.spark.demo.Demo input=<your input path> output=<your output path>
 */
object Demo {
  def main(args: Array[String]) {
    //parse the named arguments
    val namedArgs = getNamedArgs(args)
    namedArgs.keys.foreach(key=>println(key+"--->"+namedArgs(key)))

    //create a spark application
    val conf = new SparkConf().setAppName("Demo")
    val sc = new SparkContext(conf)

    //read the file from input location
    val rdd = sc.textFile(namedArgs("input"))

    //write it back to output location
    rdd.saveAsTextFile(namedArgs("output"))
  }

  def getNamedArgs(args:Array[String]):Map[String,String]={
    args.filter(line=>line.contains("="))//take only named arguments
      .map(x=>(x.substring(0,x.indexOf("=")),x.substring(x.indexOf("=")+1)))//split into key values
      .toMap//convert to a map
  }
}
