using System;

namespace CustomActivities
{
    class Program
    {
        static void Main(string[] args)
        {
            var classCreator = new ClassFactory();

            string result = "ADF_SFTP_MetadataReader";//Console.ReadLine();


            try
            {
                IClass cl = classCreator.GetClass(args[0]);
                cl.Execute();
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
    }
}
