using CascadeIDE.Services.Presentation;

using Xunit;



namespace CascadeIDE.Tests;



public sealed class PresentationCompactMainGridLayoutBuilderTests

{

    [Fact]

    public void BuildRowDefinitions_cockpit_three_rows()

    {

        Assert.Equal("Auto,Auto,*", PresentationCompactMainGridLayoutBuilder.CockpitRowDefinitions);

        Assert.Equal(

            PresentationCompactMainGridLayoutBuilder.CockpitRowDefinitions,

            PresentationCompactMainGridLayoutBuilder.BuildRowDefinitions(

                intercomBottomVisible: false,

                mfdBottomVisible: false));

    }



    [Fact]

    public void BuildRowDefinitions_single_bottom_dock_adds_splitter_row()

    {

        Assert.Equal(

            "Auto,Auto,*,4,Auto",

            PresentationCompactMainGridLayoutBuilder.BuildRowDefinitions(

                intercomBottomVisible: true,

                mfdBottomVisible: false));

        Assert.Equal(

            "Auto,Auto,*,4,Auto",

            PresentationCompactMainGridLayoutBuilder.BuildRowDefinitions(

                intercomBottomVisible: false,

                mfdBottomVisible: true));

    }



    [Fact]

    public void BuildRowDefinitions_dual_bottom_docks_stack_rows()

    {

        Assert.Equal(

            "Auto,Auto,*,4,Auto,4,Auto",

            PresentationCompactMainGridLayoutBuilder.BuildRowDefinitions(

                intercomBottomVisible: true,

                mfdBottomVisible: true));

    }



    [Fact]

    public void BuildWithRightChromeWidth_zero_collapses_right_column()

    {

        var frame = PresentationCompactMainGridLayoutBuilder.BuildWithRightChromeWidth(0, 8);

        Assert.Equal("0,4,*,4,0", frame.ColumnDefinitions);

        Assert.Equal(1, frame.ContentZoneCount);

    }



    [Fact]

    public void BuildWithRightChromeWidth_positive_allocates_forward_and_right()

    {

        var frame = PresentationCompactMainGridLayoutBuilder.BuildWithRightChromeWidth(380, 8);

        Assert.Equal("0,4,*,4,380", frame.ColumnDefinitions);

        Assert.Equal(2, frame.ContentZoneCount);

    }

}


