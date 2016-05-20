lazy val root = (project in file(".")).
  settings(
    name := "SimpleSparkAdfWithPackage",
    version := "1.0",
    scalaVersion := "2.11.7",
    libraryDependencies += "org.apache.spark" %% "spark-core" % "1.6.1",
    libraryDependencies += "org.apache.spark" %% "spark-sql" % "1.6.1",
    libraryDependencies += "com.databricks" %% "spark-csv" % "1.3.0"
  )