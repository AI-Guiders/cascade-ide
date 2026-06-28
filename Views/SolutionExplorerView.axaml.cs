using System.IO;
using Avalonia.Controls;

using Avalonia.Input;

using Avalonia.Interactivity;

using Avalonia.Threading;

using CascadeIDE.Models;

using CascadeIDE.ViewModels;



namespace CascadeIDE.Views;



public partial class SolutionExplorerView : UserControl

{

    private MainWindowViewModel? _vm;

    private TreeView? _tree;



    public SolutionExplorerView()

    {

        InitializeComponent();

        DataContextChanged += OnDataContextChanged;

        AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);

    }



    private TextBox? FilterBox => this.FindControl<TextBox>("SolutionFilterBox");



    private void OnDataContextChanged(object? sender, EventArgs e)

    {

        if (_vm is not null)

            _vm.SolutionExplorerFilterFocusRequested -= OnFilterFocusRequested;



        _vm = DataContext as MainWindowViewModel;

        if (_vm is not null)

            _vm.SolutionExplorerFilterFocusRequested += OnFilterFocusRequested;



        _tree ??= this.FindControl<TreeView>("SolutionTree");

        if (_tree is null)

            return;



        if (_vm is not null)

            _vm.RefreshSolutionExplorerTreeFilter();



        if (_treeDoubleTapHandler is null)

        {

            _treeDoubleTapHandler = OnTreeDoubleTapped;

            _tree.AddHandler(InputElement.DoubleTappedEvent, _treeDoubleTapHandler);

        }

    }



    private EventHandler<RoutedEventArgs>? _treeDoubleTapHandler;



    private void OnFilterFocusRequested()

    {

        Dispatcher.UIThread.Post(() =>

        {

            FilterBox?.Focus();

            FilterBox?.SelectAll();

        });

    }



    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)

    {

        if (_vm is null)

            return;



        if (e.Key == Key.Oem1 && e.KeyModifiers.HasFlag(KeyModifiers.Control))

        {

            FilterBox?.Focus();

            e.Handled = true;

            return;

        }



        if (e.Key == Key.Escape && FilterBox?.IsFocused == true

            && !string.IsNullOrEmpty(_vm.SolutionExplorerFilterText))

        {

            _vm.SolutionExplorerFilterText = "";

            e.Handled = true;

        }

    }



    private void OnTreeDoubleTapped(object? sender, RoutedEventArgs e)

    {

        if (_vm is null)

            return;

        if (e.Source is not Control { DataContext: SolutionItem item })

            return;

        if (item.FullPath is not { } path || Directory.Exists(path))

            return;

        _vm.SolutionExplorerSelectedItem = item;

        _vm.OpenSelectedSolutionItemCommand.Execute(null);

        e.Handled = true;

    }

}


