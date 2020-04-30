using System.IO;
using Xunit;

namespace CompilePasstroughTest
{
    public class CompilePasstroughUnitTest
    {
        [Fact]
        public void CreatesLegendFile()
        {
            var legendFile =
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "legend.txt");
            Assert.True(File.Exists(legendFile), legendFile);
        }
    }
}
