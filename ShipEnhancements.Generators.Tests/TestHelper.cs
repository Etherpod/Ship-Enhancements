using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ShipEnhancements.Generators.Tests;

public static class TestHelper
{
    public static Task VerifySource(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        IList<PortableExecutableReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        ];

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: [syntaxTree],
            references: references
        );

        var generator = new SESettingsGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation);

        return Verify(driver).UseDirectory("Snapshots");
    }
}