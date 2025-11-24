using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using static DitzyExtensions.Collection.ListExtensions;
using static ShipEnhancements.Generators.GenUtils;

namespace ShipEnhancements.Generators;

[Generator]
public class SESettingsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
            {
                ctx.AddSource("SESetting.g.cs", SourceText.From(SESettingRecordSource, Encoding.UTF8));
                ctx.AddSource("SESettingType.g.cs", SourceText.From(SESettingTypeSource, Encoding.UTF8));
                ctx.AddSource("SESettingAttribute.g.cs", SourceText.From(SESettingAttributeSource, Encoding.UTF8));
            }
        );

        var sheetPatchData = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                SESettingAttributeFqn,
                predicate: static (_, _) => true,
                transform: static (ctx, _) => GetEnumToGenerate(ctx.SemanticModel, ctx.TargetNode)
            )
            .Where(static m => m is not null);

        context.RegisterSourceOutput(
            sheetPatchData,
            static (ctx, data) => GenerateSheetPatches(ctx, data)
        );
    }

    private static void GenerateSheetPatches(SourceProductionContext ctx, SESettingsData? data)
    {
        if (data == null || data.Value.Settings.Values.IsEmpty()) return;

        ctx.AddSource(
            $"{data.Value.ClassName}.g.cs",
            SourceText.From(GetSettingsClassSource(data.Value), Encoding.UTF8)
        );
    }

    private static SESettingsData? GetEnumToGenerate(SemanticModel semanticModel, SyntaxNode targetNode)
    {
        if (semanticModel.GetDeclaredSymbol(targetNode) is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        var attributeType = semanticModel.Compilation.GetTypeByMetadataName(SESettingAttributeFqn);
        if (attributeType is null) return null;

        var settingsData = new SESettingsData();
        settingsData.ClassName = classSymbol.Name;
        settingsData.Namespace = classSymbol.ContainingNamespace.ToDisplayString();
        
        var settingData = new List<SESettingData>();
        INamedTypeSymbol? fieldType = null;
        foreach (var attributeData in classSymbol.GetAttributes())
        {
            if (!attributeType.Equals(attributeData.AttributeClass, SymbolEqualityComparer.Default))
                continue;

            var s = new SESettingData();

            var constructorArgs = attributeData.ConstructorArguments;
            for (var i = 0; i < constructorArgs.Length; i++)
            {
                var arg = constructorArgs[i];
                if (arg.Kind == TypedConstantKind.Error) return null;

                switch (i)
                {
                    case 0:
                        s.Name = (arg.Value as string)!;
                        break;
                    case 1:
                        fieldType = (arg.Value as INamedTypeSymbol)!;
                        break;
                    case 2:
                        if (arg.Kind == TypedConstantKind.Enum)
                        {
                            var ix = (arg.Value as int?)!.Value;
                            s.SettingType = arg.Type!.GetMembers()[ix].Name;
                        }
                        break;
                    case 3:
                        s.DefaultValue = GetTextForDefault(arg.Value);
                        break;
                }
            }

            foreach (var arg in attributeData.NamedArguments)
            {
                if (arg.Value.Kind == TypedConstantKind.Error) return null;

                var argVal = arg.Value.Value;
                switch (arg.Key)
                {
                    case "Name":
                        s.Name = (argVal as string)!;
                        break;
                    case "FieldType":
                        fieldType = (argVal as INamedTypeSymbol)!;
                        break;
                    case "SettingType":
                        if (arg.Value.Kind == TypedConstantKind.Enum)
                        {
                            var ix = (argVal as int?)!.Value;
                            s.SettingType = arg.Value.Type!.GetMembers()[ix].Name;
                        }
                        break;
                    case "DefaultValue":
                        s.DefaultValue = GetTextForDefault(argVal);
                        break;
                }
            }

            if (fieldType is null) continue;

            s.FieldType = fieldType.ToDisplayString();

            settingData.Add(s);
        }

        settingsData.Settings = new EqArray<SESettingData>(settingData);
        return settingsData;
    }

    private static string GetTextForDefault(object? o)
    {
        return o switch
        {
            string s => $"\"{s}\"",
            bool b => b ? "true" : "false",
            _ => o.ToString()
        };
    }
}