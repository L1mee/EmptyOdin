using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.Text.RegularExpressions.Regex;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace EmptyOdin;

public static class Methods
{
    private const string BlockComments = @"/\*(.*?)\*/";
    private const string LineComments = @"//(.*?)\r?\n";
    private const string Strings = @"""((\\[^\n]|[^""\n])*)""";
    private const string VerbatimStrings = @"@(""[^""]*"")+";

    public static void MakeNewTree(string path, string outPath)
    {
        string[] files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);

        foreach (string fileName in files)
        {
            string input = File.ReadAllText(fileName);

            //Regex rx = new Regex(@"//.*(\n)", RegexOptions.Compiled);
            //input = input.Replace(@"\/\/.*(\n)", "");
            string noComments = RemoveComments(input);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(noComments);

            SyntaxNode root = tree.GetRoot();

            //Console.WriteLine(root.SyntaxTree);
            //Console.WriteLine($"BREAK {fileName}");

            var branch = root.ChildNodes();

            foreach (var syntaxNode in branch)
            {
                //if (syntaxNode.IsKind(SyntaxKind.ConstructorConstraint))
                if (syntaxNode is UsingDirectiveSyntax)
                {
                    Console.WriteLine(syntaxNode);
                }
            }
        }
    }

    public static void RemoveNodesMethod(string path, string outPath)
    {
        string[] files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);

        foreach (string fileName in files)
        {
            string input = File.ReadAllText(fileName);

            input = RemoveComments(input);

            SyntaxTree tree = CSharpSyntaxTree.ParseText(input);
            SyntaxNode root = tree.GetRoot();

            //VariableDeclarationSyntax somehow leads to an error
            SyntaxNode? newRoot = root.RemoveNodes(root.DescendantNodes().Where(c => c is MethodDeclarationSyntax or UsingDirectiveSyntax), SyntaxRemoveOptions.KeepDirectives);
            string modifiedScript = newRoot!.ToFullString();
            string outputPath = fileName.Replace(path, outPath);

            if (!Directory.Exists(Directory.GetParent(outputPath)?.FullName))
            {
                Directory.CreateDirectory(Directory.GetParent(outputPath)!.FullName);
            }

            File.WriteAllText(outputPath, "#if !ODIN_INSPECTOR\n" + modifiedScript + "\n#endif");
        }

        Console.WriteLine(files.Length);
    }

    private static string RemoveComments(string input)
    {
        input = input.Replace("#", "//");

        string noComments = Replace(input,
            BlockComments + "|" + LineComments + "|" + Strings + "|" + VerbatimStrings,
            me => {
                if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
                    return me.Value.StartsWith("//") ? Environment.NewLine : "";
                // Keep the literal strings
                return me.Value;
            },
            RegexOptions.Singleline);
        noComments = Replace(noComments, @"^(\s*\r?\n){2,}", Environment.NewLine, RegexOptions.Multiline);
        noComments = noComments.Replace("//endif", "");
        return noComments;
    }
}