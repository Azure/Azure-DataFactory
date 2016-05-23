lazy val root = (project in file(".")).
  settings(
    name := "SimpleSparkADFDemo",
    version := "1.0",
    scalaVersion := "2.11.7",
    libraryDependencies += "org.apache.spark" %% "spark-core" % "1.6.1",
    libraryDependencies += "org.apache.spark" %% "spark-sql" % "1.6.1"
  )