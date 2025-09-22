using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace SpFormatter.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private SourcePawnFormatter? _formatter;
    private readonly DispatcherTimer? _formatTimer;
    private bool _updating = false;
    private CancellationTokenSource? _formatCancellationTokenSource;
    private IHighlightingDefinition? _sourcePawnHighlighting;

    public MainWindow()
    {
        try
        {
            _updating = true;
            InitializeComponent();
            
            // Initialize timer for delayed formatting (to avoid formatting on every keystroke)
            _formatTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500) // 500ms delay after user stops typing
            };
            _formatTimer.Tick += FormatTimer_Tick;
            
            Loaded += MainWindow_Loaded;
            _updating = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Constructor error: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdateStatus("Initializing SourcePawn formatter...");
            
            // Step 1: Set up basic text editors
            SetupBasicTextEditors();
            
            // Step 2: Skip syntax highlighting for now to ensure app works
            UpdateStatus("Syntax highlighting temporarily disabled");
            
            // Step 3: Load default content
            await LoadDefaultInputAsync();
            
            // Step 4: Initialize formatter
            var options = GetFormattingOptionsFromUI();
            _formatter = new SourcePawnFormatter(options);
            
            // Step 5: Enable real-time formatting
            InputEditor.TextChanged += InputEditor_TextChanged;
            
            UpdateStatus("Ready - SourcePawn Formatter");
            
            // Step 6: Initial formatting
            await FormatCodeAsync();
            
        }
        catch (Exception ex)
        {
            UpdateStatus($"Initialization error: {ex.Message}");
            MessageBox.Show($"Initialization error: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TryLoadSyntaxHighlighting()
    {
        try
        {
            // Try to load from embedded resource first
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "SpFormatter.UI.SourcePawn.xshd";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new XmlTextReader(stream);
                _sourcePawnHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                
                if (_sourcePawnHighlighting != null)
                {
                    HighlightingManager.Instance.RegisterHighlighting("SourcePawn", new[] { ".sp", ".inc" }, _sourcePawnHighlighting);
                    ApplySyntaxHighlighting();
                    UpdateStatus("SourcePawn syntax highlighting loaded successfully");
                    return;
                }
            }
            
            // Fallback to WPF resource
            var resourceUri = new Uri("pack://application:,,,/SourcePawn.xshd");
            var streamInfo = Application.GetResourceStream(resourceUri);
            
            if (streamInfo?.Stream != null)
            {
                using var reader = new XmlTextReader(streamInfo.Stream);
                _sourcePawnHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                
                if (_sourcePawnHighlighting != null)
                {
                    HighlightingManager.Instance.RegisterHighlighting("SourcePawn", new[] { ".sp", ".inc" }, _sourcePawnHighlighting);
                    ApplySyntaxHighlighting();
                    UpdateStatus("SourcePawn syntax highlighting loaded successfully (WPF resource)");
                    return;
                }
            }
            
            UpdateStatus("SourcePawn syntax highlighting not available - using plain text");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Syntax highlighting disabled: {ex.Message}");
            // Continue without syntax highlighting - don't crash the app
        }
    }

    private void ApplySyntaxHighlighting()
    {
        if (_sourcePawnHighlighting != null)
        {
            InputEditor.SyntaxHighlighting = _sourcePawnHighlighting;
            OutputEditor.SyntaxHighlighting = _sourcePawnHighlighting;
        }
    }

    private void LoadSyntaxHighlighting()
    {
        try
        {
            // Try to load the WPF resource
            var resourceUri = new Uri("pack://application:,,,/SourcePawn.xshd");
            var streamInfo = Application.GetResourceStream(resourceUri);
            
            if (streamInfo?.Stream == null)
            {
                UpdateStatus("SourcePawn.xshd resource not found - using default highlighting");
                return;
            }

            using var reader = new XmlTextReader(streamInfo.Stream);
            _sourcePawnHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            
            if (_sourcePawnHighlighting != null)
            {
                HighlightingManager.Instance.RegisterHighlighting("SourcePawn", new[] { ".sp", ".inc" }, _sourcePawnHighlighting);
                UpdateStatus("SourcePawn syntax highlighting loaded successfully");
            }
            else
            {
                UpdateStatus("Failed to create highlighting definition from XSHD");
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Could not load syntax highlighting: {ex.Message}");
            // Don't show message box - just log and continue without syntax highlighting
        }
    }

    private void SetupTextEditors()
    {
        try
        {
            // Set up input editor
            InputEditor.SyntaxHighlighting = _sourcePawnHighlighting;
            InputEditor.Options.EnableHyperlinks = false;
            InputEditor.Options.EnableEmailHyperlinks = false;
            InputEditor.TextChanged += InputEditor_TextChanged;
            
            // Set up output editor
            OutputEditor.SyntaxHighlighting = _sourcePawnHighlighting;
            OutputEditor.Options.EnableHyperlinks = false;
            OutputEditor.Options.EnableEmailHyperlinks = false;
            OutputEditor.Text = "Formatted code will appear here...";
            
            UpdateStatus("Text editors setup completed");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Text editor setup error: {ex.Message}");
            MessageBox.Show($"Text editor setup error: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Editor Setup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SetupTextEditorsWithHighlighting()
    {
        try
        {
            // Basic options
            InputEditor.Options.EnableHyperlinks = false;
            InputEditor.Options.EnableEmailHyperlinks = false;
            InputEditor.TextChanged += InputEditor_TextChanged;
            
            OutputEditor.Options.EnableHyperlinks = false;
            OutputEditor.Options.EnableEmailHyperlinks = false;
            
            // Apply syntax highlighting if available
            if (_sourcePawnHighlighting != null)
            {
                InputEditor.SyntaxHighlighting = _sourcePawnHighlighting;
                OutputEditor.SyntaxHighlighting = _sourcePawnHighlighting;
                UpdateStatus("Text editors setup completed with SourcePawn syntax highlighting");
            }
            else
            {
                UpdateStatus("Text editors setup completed (no syntax highlighting)");
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Text editor setup error: {ex.Message}");
            // Continue without syntax highlighting if there's an error
        }
    }

    private void SetupBasicTextEditors()
    {
        try
        {
            // Set up input editor
            InputEditor.Options.EnableHyperlinks = false;
            InputEditor.Options.EnableEmailHyperlinks = false;
            InputEditor.ShowLineNumbers = true;
            InputEditor.WordWrap = false;
            
            // Set up output editor
            OutputEditor.Options.EnableHyperlinks = false;
            OutputEditor.Options.EnableEmailHyperlinks = false;
            OutputEditor.IsReadOnly = true;
            OutputEditor.ShowLineNumbers = true;
            OutputEditor.WordWrap = false;
            
            UpdateStatus("Text editors configured");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Text editor setup error: {ex.Message}");
            throw; // Re-throw to be caught by the caller
        }
    }

    private void SetupTextEditorsBasic()
    {
        try
        {
            // Set up editors without syntax highlighting
            InputEditor.Options.EnableHyperlinks = false;
            InputEditor.Options.EnableEmailHyperlinks = false;
            InputEditor.TextChanged += InputEditor_TextChanged;
            
            OutputEditor.Options.EnableHyperlinks = false;
            OutputEditor.Options.EnableEmailHyperlinks = false;
            
            UpdateStatus("Text editors setup completed (no syntax highlighting)");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Text editor setup error: {ex.Message}");
            MessageBox.Show($"Text editor setup error: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Editor Setup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadDefaultInputAsync()
    {
        try
        {
            var testFilePath = "input.sp";
            if (File.Exists(testFilePath))
            {
                var content = await File.ReadAllTextAsync(testFilePath);
                _updating = true;
                try
                {
                    InputEditor.Text = content;
                }
                finally
                {
                    _updating = false;
                }
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Could not load test file: {ex.Message}");
            // Keep the default simple content if file loading fails
        }
    }

    private FormattingOptions GetFormattingOptionsFromUI()
    {
        return new FormattingOptions
        {
            // Core indentation and spacing options
            IndentSize = int.TryParse(IndentSizeTextBox.Text, out var size) ? size : 4,
            UseSpaces = UseSpacesCheckBox.IsChecked == true,
            SpaceAfterComma = SpaceAfterCommaCheckBox.IsChecked == true,
            SpaceAroundOperators = SpaceAroundOperatorsCheckBox.IsChecked == true,
            SpaceBeforeOpenParen = SpaceBeforeOpenParenCheckBox.IsChecked == true,
            SpaceAfterSemicolon = SpaceAfterSemicolonCheckBox.IsChecked == true,
            SpaceInArrayBrackets = SpaceInArrayBracketsCheckBox.IsChecked == true,
            
            // Line break options
            NewLineAfterOpenBrace = NewLineAfterOpenBraceCheckBox.IsChecked == true,
            NewLineBeforeCloseBrace = NewLineBeforeCloseBraceCheckBox.IsChecked == true,
            NewLineAfterSemicolon = NewLineAfterSemicolonCheckBox.IsChecked == true,
            NewLineAfterInclude = NewLineAfterIncludeCheckBox.IsChecked == true,
            MaxLineLength = int.TryParse(MaxLineLengthTextBox.Text, out var length) ? length : 120,
            
            // Advanced formatting options
            PreserveEmptyLines = PreserveEmptyLinesCheckBox.IsChecked == true,
            MaxConsecutiveEmptyLines = int.TryParse(MaxConsecutiveEmptyLinesTextBox.Text, out var maxEmptyLines) ? maxEmptyLines : 2,
            SortIncludes = SortIncludesCheckBox.IsChecked == true,
            IndentPreprocessor = IndentPreprocessorCheckBox.IsChecked == true,
            AlignConsecutiveAssignments = AlignConsecutiveAssignmentsCheckBox.IsChecked == true,
            AlignConsecutiveDeclarations = AlignConsecutiveDeclarationsCheckBox.IsChecked == true,
            
            // SourcePawn-specific options
            CompactFunctionParameters = CompactFunctionParametersCheckBox.IsChecked == true,
            RequireSemicolons = RequireSemicolonsCheckBox.IsChecked == true,
            RemoveOptionalSemicolons = RemoveOptionalSemicolonsCheckBox.IsChecked == true,
            LineEnding = GetSelectedLineEnding()
        };
    }
    
    private string GetSelectedLineEnding()
    {
        if (LineEndingComboBox.SelectedIndex == 1) // CRLF
            return "\r\n";
        return "\n"; // LF (default)
    }

    private void InputEditor_TextChanged(object? sender, EventArgs e)
    {
        if (_updating) return;
        
        // Format immediately on every keystroke
        _formatTimer?.Stop();
        _ = FormatCodeAsync();
    }

    private void OptionsChanged(object sender, RoutedEventArgs e)
    {
        if (_updating) return;
        
        try
        {
            // Recreate formatter with new options
            _formatter?.Dispose();
            var options = GetFormattingOptionsFromUI();
            _formatter = new SourcePawnFormatter(options);
            
            // Trigger immediate formatting when options change
            _formatTimer?.Stop();
            _ = FormatCodeAsync();
        }
        catch (Exception ex)
        {
            UpdateStatus($"Options error: {ex.Message}");
        }
    }

    private void FormatTimer_Tick(object? sender, EventArgs e)
    {
        _formatTimer?.Stop();
        _ = FormatCodeAsync();
    }

    private async Task FormatCodeAsync()
    {
        if (_formatter == null || _updating) return;

        // Cancel any existing formatting operation
        _formatCancellationTokenSource?.Cancel();
        _formatCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _formatCancellationTokenSource.Token;

        try
        {
            var inputText = InputEditor.Text;
            
            if (string.IsNullOrWhiteSpace(inputText))
            {
                OutputEditor.Text = string.Empty;
                UpdateStatus("Ready");
                return;
            }

            UpdateStatus("Formatting...");
            
            // Run formatting on background thread to avoid blocking UI
            var formattedCode = await Task.Run(() => 
            {
                // Check for cancellation before expensive operation
                cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    return _formatter.Format(inputText);
                }
                catch (FormatException ex)
                {
                    // Show detailed syntax error information
                    return $"=== FORMATTING ERROR ===\n{ex.Message}\n\n=== ORIGINAL CODE ===\n{inputText}";
                }
                catch (Exception ex)
                {
                    return $"=== UNEXPECTED ERROR ===\n{ex.GetType().Name}: {ex.Message}\n\n=== ORIGINAL CODE ===\n{inputText}";
                }
            }, cancellationToken);

            // Check for cancellation before updating UI
            cancellationToken.ThrowIfCancellationRequested();

            // Update UI on main thread
            _updating = true;
            try
            {
                OutputEditor.Text = formattedCode;
                UpdateStatus("Ready");
            }
            finally
            {
                _updating = false;
            }
        }
        catch (OperationCanceledException)
        {
            // Formatting was cancelled - this is normal during rapid typing
            return;
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error: {ex.Message}");
            OutputEditor.Text = $"Unexpected error:\n{ex.Message}";
        }
    }

    private void UpdateStatus(string message)
    {
        StatusText.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
    }

    protected override void OnClosed(EventArgs e)
    {
        _formatTimer?.Stop();
        _formatCancellationTokenSource?.Cancel();
        _formatCancellationTokenSource?.Dispose();
        _formatter?.Dispose();
        base.OnClosed(e);
    }
}