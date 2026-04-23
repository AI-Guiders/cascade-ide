using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.ArchitectureAnalyzers;

/// <summary>
/// Сдерживает размер <c>Views/MfdShellView.axaml</c> и <c>Views/MfdShellPageStack.axaml</c>: тонкий маршрутизатор + набор оверлей-страниц, без тяжёлой вёрстки.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MfdShellViewXamlLeanAnalyzer : DiagnosticAnalyzer
{
    public const string TooLongId = "CASCOPE017";
    public const string InlineControlsId = "CASCOPE018";

    public const string ShellViewFileName = "MfdShellView.axaml";
    public const string PageStackFileName = "MfdShellPageStack.axaml";

    /// <summary>Только EICAS + <c>BottomPanelShell</c> + <c>MfdShellPageStack</c>.</summary>
    public const int MaxLineCountMfdShellView = 48;

    /// <summary>Конвертер + набор <c>Border</c>+страница; растёт с числом Mfd-страниц.</summary>
    public const int MaxLineCountMfdShellPageStack = 150;

    private static readonly DiagnosticDescriptor TooLongRule = new(
        TooLongId,
        "Mfd shell XAML: слишком много строк",
        "Файл {0} имеет {1} строк (максимум {2}). См. CASCOPE017 в README анализаторов.",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "MfdShellView — каркас; MfdShellPageStack — набор страниц Mfd. Детальная вёрстка в *MfdPageView.",
        helpLinkUri: null,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    private static readonly DiagnosticDescriptor InlineControlsRule = new(
        InlineControlsId,
        "Mfd shell XAML: запрещённая inline-разметка",
        "В {0} обнаружен шаблон «{1}». Переноси интерактив и списки в *MfdPageView.",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Тяжёлые контролы (ListBox, TextBox, ItemsControl, …) не держатся в Mfd shell XAML — только *MfdPageView.",
        helpLinkUri: null,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    private static readonly (string Token, string Label)[] ForbiddenSubstrings =
    {
        ("<ListBox", "ListBox"),
        ("<DataTemplate", "DataTemplate"),
        ("<TextBox", "TextBox"),
        ("<ItemsControl", "ItemsControl"),
        ("<GridSplitter", "GridSplitter"),
        ("ColumnDefinitions=\"", "ColumnDefinitions (многостолбцовая сетка)"),
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(TooLongRule, InlineControlsRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeMfdShell);
    }

    private static void AnalyzeMfdShell(CompilationAnalysisContext context)
    {
        foreach (var file in context.Options.AdditionalFiles)
        {
            var name = System.IO.Path.GetFileName(file.Path);
            var max = name switch
            {
                ShellViewFileName => MaxLineCountMfdShellView,
                PageStackFileName => MaxLineCountMfdShellPageStack,
                _ => (int?)null
            };
            if (max is null)
                continue;

            var text = file.GetText(context.CancellationToken);
            if (text is null)
                continue;

            var content = text.ToString();
            var lineCount = text.Lines.Count;
            if (lineCount > max)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        TooLongRule,
                        CreateLocationForLine(file.Path, text, 0),
                        name,
                        lineCount,
                        max));
            }

            foreach (var (token, label) in ForbiddenSubstrings)
            {
                var idx = content.IndexOf(token, StringComparison.Ordinal);
                if (idx < 0)
                    continue;
                context.ReportDiagnostic(
                    Diagnostic.Create(InlineControlsRule, CreateLocationForOffset(file.Path, text, idx), name, label));
            }
        }
    }

    private static Location CreateLocationForLine(string path, SourceText text, int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= text.Lines.Count)
            return Location.None;

        var line = text.Lines[lineIndex];
        return Location.Create(path, line.Span, new LinePositionSpan(
            new LinePosition(lineIndex, 0),
            new LinePosition(lineIndex, 0)));
    }

    private static Location CreateLocationForOffset(string path, SourceText text, int offset)
    {
        if (offset < 0 || offset > text.Length)
            return Location.None;
        var pos = text.Lines.GetLinePosition(offset);
        return Location.Create(path, new TextSpan(offset, 0), new LinePositionSpan(pos, pos));
    }
}
