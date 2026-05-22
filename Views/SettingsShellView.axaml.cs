using Avalonia.Controls;
using Avalonia.Interactivity;
using CascadeIDE.Features.Settings;
using CascadeIDE.Services;
using CascadeIDE.Views.Settings;

namespace CascadeIDE.Views;

public partial class SettingsShellView : UserControl
{
    private readonly Dictionary<string, Control> _panelCache = new(StringComparer.OrdinalIgnoreCase);

    public SettingsShellView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        CategoryList.SelectionChanged += OnCategorySelectionChanged;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var nav = BuildNavigation(SettingsPanelRegistry.LoadOrdered());
        CategoryList.ItemsSource = nav;
        var first = nav.OfType<SettingsNavigationCategory>().FirstOrDefault();
        if (first is not null)
            CategoryList.SelectedItem = first;
        else
            ShowCategory(null);
    }

    internal static IReadOnlyList<SettingsNavigationItem> BuildNavigation(
        IReadOnlyList<Models.SettingsCategoryDefinition> categories)
    {
        var items = new List<SettingsNavigationItem>();
        string? lastGroup = null;
        foreach (var cat in categories.OrderBy(c => c.Order).ThenBy(c => c.Title, StringComparer.OrdinalIgnoreCase))
        {
            var group = cat.Group.Trim();
            if (!string.IsNullOrEmpty(group)
                && !string.Equals(group, lastGroup, StringComparison.Ordinal))
            {
                items.Add(new SettingsNavigationGroupHeader(group));
                lastGroup = group;
            }

            items.Add(new SettingsNavigationCategory(
                cat.Id,
                group,
                cat.Title,
                cat.Panel,
                ShowGroupHeader: false));
        }

        return items;
    }

    private void OnCategorySelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (CategoryList.SelectedItem is SettingsNavigationGroupHeader)
        {
            SelectFirstCategoryAfterHeader();
            return;
        }

        if (CategoryList.SelectedItem is SettingsNavigationCategory cat)
            ShowCategory(cat);
    }

    private void SelectFirstCategoryAfterHeader()
    {
        if (CategoryList.ItemsSource is not IEnumerable<SettingsNavigationItem> items)
            return;

        var list = items.ToList();
        var idx = CategoryList.SelectedIndex;
        if (idx < 0)
            idx = 0;
        for (var i = idx; i < list.Count; i++)
        {
            if (list[i] is SettingsNavigationCategory cat)
            {
                CategoryList.SelectedItem = cat;
                return;
            }
        }
    }

    private void ShowCategory(SettingsNavigationCategory? category)
    {
        if (category is null)
        {
            PageTitle.Text = "";
            PageHost.Content = null;
            return;
        }

        PageTitle.Text = category.Title;
        PageHost.Content = ResolvePanel(category.Panel);
    }

    private Control? ResolvePanel(string panelId)
    {
        if (_panelCache.TryGetValue(panelId, out var cached))
            return cached;

        var created = SettingsPanelFactory.TryCreate(panelId);
        if (created is null)
            return null;

        created.DataContext = DataContext;
        _panelCache[panelId] = created;
        return created;
    }
}
