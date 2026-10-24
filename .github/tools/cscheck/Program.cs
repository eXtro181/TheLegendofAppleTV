// Syntaxprüfung der Unity-Skripte ohne Unity (findet Tippfehler vor dem eigentlichen Build).
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
int errors = 0;
foreach (var f in Directory.GetFiles(args[0], "*.cs", SearchOption.AllDirectories))
{
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(f), new CSharpParseOptions(LanguageVersion.CSharp9));
    foreach (var d in tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)) { Console.WriteLine($"{Path.GetFileName(f)}{d.Location.GetLineSpan().StartLinePosition}: {d.GetMessage()}"); errors++; }
}
Console.WriteLine(errors == 0 ? "Syntax ok" : errors + " Fehler");
return errors == 0 ? 0 : 1;
