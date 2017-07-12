using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jsreport.VSTools
{
    public static class Constants
    {
        public static string CSPROJ_UPDATE = @"
  <ItemGroup>
    <None Update=""jsreport\**\*.*"">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

  <ItemGroup>
    <None Update=""jsreport\jsreport.Local"">
      <CopyToOutputDirectory>Never</CopyToOutputDirectory>
    </None>
  </ItemGroup>
 
</Project>
";

        public static string JSREPORT_CONFIG = @"{ 
    ""connectionString"": { ""name"": ""fs"" }, 
    ""httpPort"": 5488, 
    ""sample-template"": { ""createSamples"": true } 
}";
    }
}
