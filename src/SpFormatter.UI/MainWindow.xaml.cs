using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;

namespace SpFormatter.UI;

public partial class MainWindow : Window
{
    private enum OutputMode
    {
        Formatted,
        Ast,
        Errors
    }

    private readonly DispatcherTimer _debounceTimer;
    private CancellationTokenSource? _refreshCts;
    private IHighlightingDefinition? _sourcePawnHighlighting;
    private bool _updating;
    private bool _ready;
    private string? _currentPath;
    private string _lastFormattedText = string.Empty;

    public MainWindow()
    {
        InitializeComponent();

        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(280)
        };
        _debounceTimer.Tick += (_, _) =>
        {
            _debounceTimer.Stop();
            _ = RefreshOutputAsync(force: true);
        };

        PreviewKeyDown += MainWindow_PreviewKeyDown;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ConfigureEditors();
            TryLoadSyntaxHighlighting();
            await LoadDefaultInputAsync();
            _ready = true;
            await RefreshOutputAsync(force: true);
            UpdateStatus("Ready");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Init failed: {ex.Message}");
            MessageBox.Show(this, ex.Message, "SpFormatter", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ConfigureEditors()
    {
        foreach (var editor in new[] { InputEditor, OutputEditor })
        {
            editor.Options.EnableHyperlinks = false;
            editor.Options.EnableEmailHyperlinks = false;
            editor.Options.AllowScrollBelowDocument = true;
            editor.ShowLineNumbers = true;
            editor.WordWrap = false;
        }

        InputEditor.TextChanged += InputEditor_TextChanged;
        ApplyEditorTheme(DarkEditorsToggle.IsChecked == true);
    }

    private void TryLoadSyntaxHighlighting()
    {
        try
        {
            _sourcePawnHighlighting = DarkEditorsToggle.IsChecked == true
                ? SourcePawnHighlighting.CreateDark()
                : SourcePawnHighlighting.CreateLight();

            // Validate so a bad rule fails here instead of crashing layout.
            var probeDoc = new TextDocument("#include <sourcemod>\nvoid t() { PrintToServer(\"hi\"); }\n");
            var probeHighlighter = new DocumentHighlighter(probeDoc, _sourcePawnHighlighting);
            _ = probeHighlighter.HighlightLine(1);
            _ = probeHighlighter.HighlightLine(2);

            HighlightingManager.Instance.RegisterHighlighting("SourcePawn", [".sp", ".inc"], _sourcePawnHighlighting);
            ApplySourceHighlightingToEditors();
        }
        catch (Exception ex)
        {
            _sourcePawnHighlighting = null;
            InputEditor.SyntaxHighlighting = null;
            OutputEditor.SyntaxHighlighting = null;
            UpdateStatus($"Highlighting disabled: {ex.Message}");
        }
    }

    private void ApplyEditorTheme(bool dark)
    {
        var background = (Brush)FindResource(dark ? "BgEditorDarkBrush" : "BgEditorBrush");
        var foreground = (Brush)FindResource(dark ? "InkDarkBrush" : "InkBrush");
        var lineNumbers = (Brush)FindResource(dark ? "MutedDarkBrush" : "MutedBrush");

        foreach (var editor in new[] { InputEditor, OutputEditor })
        {
            editor.Background = background;
            editor.Foreground = foreground;
            editor.LineNumbersForeground = lineNumbers;
            editor.TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(0x66, 0x26, 0x4F, 0x78));
            editor.TextArea.SelectionForeground = null;
            editor.TextArea.SelectionBorder = null;
        }
    }

    private void ApplySourceHighlightingToEditors()
    {
        var mode = GetSelectedOutputMode();
        InputEditor.SyntaxHighlighting = _sourcePawnHighlighting;
        OutputEditor.SyntaxHighlighting = mode == OutputMode.Formatted ? _sourcePawnHighlighting : null;
    }

    private void DarkEditors_Changed(object sender, RoutedEventArgs e)
    {
        if (!_ready)
            return;

        ApplyEditorTheme(DarkEditorsToggle.IsChecked == true);
        TryLoadSyntaxHighlighting();
        _ = RefreshOutputAsync(force: true);
    }

    private async Task LoadDefaultInputAsync()
    {
        var testFilePath = Path.Combine(AppContext.BaseDirectory, "input.sp");
        if (!File.Exists(testFilePath))
            return;

        var content = await File.ReadAllTextAsync(testFilePath);
        _updating = true;
        try
        {
            InputEditor.Text = content;
            _currentPath = testFilePath;
        }
        finally
        {
            _updating = false;
        }
    }

    private FormattingOptions GetFormattingOptionsFromUI() =>
        new()
        {
            IndentSize = int.TryParse(IndentSizeTextBox.Text, out var size) ? size : 4,
            UseSpaces = UseSpacesCheckBox.IsChecked == true,
            SpaceAfterComma = SpaceAfterCommaCheckBox.IsChecked == true,
            SpaceAroundOperators = SpaceAroundOperatorsCheckBox.IsChecked == true,
            SpaceBeforeOpenParen = SpaceBeforeOpenParenCheckBox.IsChecked == true,
            SpaceInArrayBrackets = SpaceInArrayBracketsCheckBox.IsChecked == true,
            NewLineAfterOpenBrace = NewLineAfterOpenBraceCheckBox.IsChecked == true,
            NewLineAfterInclude = NewLineAfterIncludeCheckBox.IsChecked == true,
            MaxLineLength = int.TryParse(MaxLineLengthTextBox.Text, out var length) ? length : 120,
            PreserveEmptyLines = PreserveEmptyLinesCheckBox.IsChecked == true,
            MaxConsecutiveEmptyLines = int.TryParse(MaxConsecutiveEmptyLinesTextBox.Text, out var maxEmptyLines)
                ? maxEmptyLines
                : 2,
            SortIncludes = SortIncludesCheckBox.IsChecked == true,
            RequireSemicolons = RequireSemicolonsCheckBox.IsChecked == true,
            AllowSyntaxRecovery = AllowSyntaxRecoveryCheckBox.IsChecked == true,
            LineEnding = LineEndingComboBox.SelectedIndex == 1 ? "\r\n" : "\n"
        };

    private OutputMode GetSelectedOutputMode()
    {
        if (ModeAstToggle.IsChecked == true)
            return OutputMode.Ast;
        if (ModeErrorsToggle.IsChecked == true)
            return OutputMode.Errors;
        return OutputMode.Formatted;
    }

    private void InputEditor_TextChanged(object? sender, EventArgs e)
    {
        if (_updating || !_ready)
            return;

        if (LiveFormatToggle.IsChecked != true)
        {
            UpdateStatus("Live off · press Format");
            return;
        }

        _debounceTimer.Stop();
        _debounceTimer.Start();
        UpdateStatus("Typing…");
    }

    private void OptionsChanged(object sender, RoutedEventArgs e)
    {
        if (_updating || !_ready)
            return;

        if (LiveFormatToggle.IsChecked == true)
        {
            _debounceTimer.Stop();
            _ = RefreshOutputAsync(force: true);
        }
        else
        {
            UpdateStatus("Options changed · press Format");
        }
    }

    private void LiveFormat_Changed(object sender, RoutedEventArgs e)
    {
        if (!_ready)
            return;

        if (LiveFormatToggle.IsChecked == true)
            _ = RefreshOutputAsync(force: true);
        else
            UpdateStatus("Live off · press Format");
    }

    private void OutputMode_Checked(object sender, RoutedEventArgs e)
    {
        if (!_ready || _updating || sender is not ToggleButton checkedButton || checkedButton.IsChecked != true)
            return;

        _updating = true;
        try
        {
            ModeFormattedToggle.IsChecked = ReferenceEquals(checkedButton, ModeFormattedToggle);
            ModeAstToggle.IsChecked = ReferenceEquals(checkedButton, ModeAstToggle);
            ModeErrorsToggle.IsChecked = ReferenceEquals(checkedButton, ModeErrorsToggle);
        }
        finally
        {
            _updating = false;
        }

        _ = RefreshOutputAsync(force: true);
    }

    private void OutputMode_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!_ready || _updating)
            return;

        if (ModeFormattedToggle.IsChecked == true
            || ModeAstToggle.IsChecked == true
            || ModeErrorsToggle.IsChecked == true)
        {
            return;
        }

        _updating = true;
        try
        {
            if (sender is ToggleButton button)
                button.IsChecked = true;
        }
        finally
        {
            _updating = false;
        }
    }

    private void FormatNow_Click(object sender, RoutedEventArgs e) => _ = RefreshOutputAsync(force: true);

    private void ApplyOutput_Click(object sender, RoutedEventArgs e) => ApplyFormattedToInput();

    private void CopyOutput_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(OutputEditor.Text))
            return;

        Clipboard.SetText(OutputEditor.Text);
        UpdateStatus("Copied output");
    }

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "SourcePawn (*.sp;*.inc)|*.sp;*.inc|All files (*.*)|*.*",
            Title = "Open SourcePawn file"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        var text = await File.ReadAllTextAsync(dialog.FileName);
        _updating = true;
        try
        {
            InputEditor.Text = text;
            _currentPath = dialog.FileName;
        }
        finally
        {
            _updating = false;
        }

        Title = $"SpFormatter · {Path.GetFileName(dialog.FileName)}";
        await RefreshOutputAsync(force: true);
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var path = _currentPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            var dialog = new SaveFileDialog
            {
                Filter = "SourcePawn (*.sp)|*.sp|Include (*.inc)|*.inc|All files (*.*)|*.*",
                Title = "Save input",
                FileName = Path.GetFileName(path) ?? "untitled.sp"
            };
            if (dialog.ShowDialog(this) != true)
                return;
            path = dialog.FileName;
            _currentPath = path;
        }

        await File.WriteAllTextAsync(path, InputEditor.Text);
        Title = $"SpFormatter · {Path.GetFileName(path)}";
        UpdateStatus("Saved input");
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Enter)
        {
            _ = RefreshOutputAsync(force: true);
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O)
        {
            Open_Click(sender, e);
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
        {
            Save_Click(sender, e);
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.C)
        {
            CopyOutput_Click(sender, e);
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.L)
        {
            ApplyFormattedToInput();
            e.Handled = true;
        }
    }

    private void ApplyFormattedToInput()
    {
        if (GetSelectedOutputMode() != OutputMode.Formatted || string.IsNullOrEmpty(_lastFormattedText))
        {
            UpdateStatus("Apply needs a successful Formatted result");
            return;
        }

        _updating = true;
        try
        {
            InputEditor.Text = _lastFormattedText;
        }
        finally
        {
            _updating = false;
        }

        _ = RefreshOutputAsync(force: true);
        UpdateStatus("Applied formatted output to input");
    }

    private async Task RefreshOutputAsync(bool force)
    {
        if (!_ready || _updating)
            return;

        if (!force && LiveFormatToggle.IsChecked != true)
            return;

        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        var token = _refreshCts.Token;
        var mode = GetSelectedOutputMode();
        var inputText = InputEditor.Text;

        try
        {
            if (string.IsNullOrWhiteSpace(inputText))
            {
                OutputEditor.Text = string.Empty;
                _lastFormattedText = string.Empty;
                SetChip(ParseChip, ParseStatusText, "parse —", "MutedBrush");
                SetChip(FormatChip, FormatStatusText, "format —", "MutedBrush");
                SetChip(ChangeChip, ChangeStatusText, "diff —", "MutedBrush");
                OutputTitleText.Text = "OUTPUT";
                UpdateStatus("Ready");
                return;
            }

            UpdateStatus(mode switch
            {
                OutputMode.Ast => "Parsing…",
                OutputMode.Errors => "Collecting errors…",
                _ => "Formatting…"
            });

            var options = GetFormattingOptionsFromUI();
            var snapshot = await Task.Run(() => BuildOutputSnapshot(inputText, mode, options), token);
            token.ThrowIfCancellationRequested();

            _updating = true;
            try
            {
                OutputEditor.Text = snapshot.Text;
                if (mode == OutputMode.Formatted && snapshot.FormatSucceeded)
                    _lastFormattedText = snapshot.FormattedText;
                else if (mode == OutputMode.Formatted)
                    _lastFormattedText = string.Empty;

                OutputTitleText.Text = mode switch
                {
                    OutputMode.Ast => "AST",
                    OutputMode.Errors => "ERRORS",
                    _ => "OUTPUT"
                };

                SetChip(
                    ParseChip,
                    ParseStatusText,
                    snapshot.ParseStatus,
                    snapshot.HasErrors ? "ErrBrush" : "OkBrush");
                SetChip(
                    FormatChip,
                    FormatStatusText,
                    snapshot.FormatStatus,
                    snapshot.FormatSucceeded ? "OkBrush" : "ErrBrush");
                SetChip(
                    ChangeChip,
                    ChangeStatusText,
                    snapshot.ChangeStatus,
                    snapshot.ChangeBrushKey);

                ApplySourceHighlightingToEditors();
                UpdateStatus(snapshot.Status);
            }
            finally
            {
                _updating = false;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error: {ex.Message}");
            OutputEditor.Text = $"Unexpected error:\n{ex.Message}";
            SetChip(FormatChip, FormatStatusText, "format error", "ErrBrush");
        }
    }

    private void SetChip(Border chip, TextBlock label, string text, string brushKey)
    {
        label.Text = text;
        label.Foreground = (Brush)FindResource(brushKey);
        chip.Opacity = 1;
    }

    private static OutputSnapshot BuildOutputSnapshot(
        string inputText,
        OutputMode mode,
        FormattingOptions options)
    {
        using var parser = new SourcePawnParser();
        using var tree = parser.ParseSource(inputText);
        var errors = parser.GetSyntaxErrors(inputText);
        var hasErrors = tree?.RootNode?.HasError == true || errors.Count > 0;
        var parseStatus = hasErrors
            ? $"parse {Math.Max(errors.Count, 1)} err"
            : "parse ok";

        return mode switch
        {
            OutputMode.Ast => new OutputSnapshot(
                Text: tree?.RootNode == null
                    ? "(no parse tree)"
                    : AstInspector.FormatTreeStructure(tree.RootNode),
                FormattedText: string.Empty,
                ParseStatus: parseStatus,
                FormatStatus: "format —",
                FormatSucceeded: false,
                ChangeStatus: "diff —",
                ChangeBrushKey: "MutedBrush",
                HasErrors: hasErrors,
                Status: hasErrors ? "AST ready (errors)" : "AST ready"),
            OutputMode.Errors => new OutputSnapshot(
                Text: FormatErrors(errors, hasErrors),
                FormattedText: string.Empty,
                ParseStatus: parseStatus,
                FormatStatus: "format —",
                FormatSucceeded: false,
                ChangeStatus: "diff —",
                ChangeBrushKey: "MutedBrush",
                HasErrors: hasErrors,
                Status: hasErrors ? $"{errors.Count} syntax error(s)" : "No syntax errors"),
            _ => FormatFormattedSnapshot(inputText, options, parseStatus, hasErrors)
        };
    }

    private static OutputSnapshot FormatFormattedSnapshot(
        string inputText,
        FormattingOptions options,
        string parseStatus,
        bool hasErrors)
    {
        using var formatter = new SourcePawnFormatter(options);
        var result = formatter.FormatWithResult(inputText);
        if (!result.Success)
        {
            var details = result.Errors.Count == 0
                ? "Formatting failed."
                : string.Join("\n\n", result.Errors.Select(e => e.GetDetailedDescription()));
            return new OutputSnapshot(
                Text: $"=== FORMATTING FAILED (fail closed) ===\n{details}",
                FormattedText: string.Empty,
                ParseStatus: parseStatus,
                FormatStatus: "format blocked",
                FormatSucceeded: false,
                ChangeStatus: "diff —",
                ChangeBrushKey: "MutedBrush",
                HasErrors: true,
                Status: "Format blocked by syntax errors");
        }

        var normalizedInput = NormalizeForCompare(inputText, options.LineEnding);
        var normalizedOutput = NormalizeForCompare(result.Text, options.LineEnding);
        var changed = !string.Equals(normalizedInput, normalizedOutput, StringComparison.Ordinal);

        return new OutputSnapshot(
            Text: result.Text,
            FormattedText: result.Text,
            ParseStatus: parseStatus,
            FormatStatus: "format ok",
            FormatSucceeded: true,
            ChangeStatus: changed ? "would change" : "unchanged",
            ChangeBrushKey: changed ? "WarnBrush" : "OkBrush",
            HasErrors: hasErrors,
            Status: changed ? "Formatted · differs from input" : "Formatted · already clean");
    }

    private static string NormalizeForCompare(string text, string _) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd();

    private static string FormatErrors(IReadOnlyList<SyntaxError> errors, bool hasErrors)
    {
        if (errors.Count == 0)
        {
            return hasErrors
                ? "Root HasError is true, but no detailed ERROR/MISSING nodes were collected."
                : "No syntax errors.";
        }

        return string.Join("\n\n", errors.Select((e, i) => $"[{i + 1}] {e.GetDetailedDescription()}"));
    }

    private void UpdateStatus(string message) =>
        StatusText.Text = $"{DateTime.Now:HH:mm:ss} · {message}";

    protected override void OnClosed(EventArgs e)
    {
        _debounceTimer.Stop();
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        base.OnClosed(e);
    }

    private sealed record OutputSnapshot(
        string Text,
        string FormattedText,
        string ParseStatus,
        string FormatStatus,
        bool FormatSucceeded,
        string ChangeStatus,
        string ChangeBrushKey,
        bool HasErrors,
        string Status);
}
