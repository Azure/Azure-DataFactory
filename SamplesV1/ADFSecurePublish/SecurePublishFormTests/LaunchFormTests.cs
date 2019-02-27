using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ADF.Deployment.AdfKeyVaultDeployment;
using NUnit.Framework;
using SecurePublishForm;

namespace SecurePublishFormTests
{
    public class LaunchFormTests
    {
        [Test]
        [STAThread]
        public void LaunchForm()
        {
            MainWindow window = new MainWindow(@"C:\Users\daosul\Documents\Visual Studio 2015\Projects\DF\DataFactoryApp\DataFactoryApp.dfproj");
            window.ShowDialog();
        }
    }
}
