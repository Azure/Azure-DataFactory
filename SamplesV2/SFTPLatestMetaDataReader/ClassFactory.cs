using System;
using System.Linq;
using System.Reflection;

namespace CustomActivities
{
    internal class ClassFactory
    {
             public IClass GetClass(string type)
        {

            //Get the namespace
            var assembly = Assembly.GetExecutingAssembly();

            //Get all the class types in the namespace and only select the matching class type
            var objectType = assembly.GetTypes().First(t => t.Name == type);

            //Create an instance of that class type
            var obj = Activator.CreateInstance(objectType);

            //Cast the created instance
            return (IClass)obj;
        }
    }
}
