using Models.Event;
using Services.Implementation;
using Xunit;

namespace UnitTests.Services;

public class TcnOralNotificationServicePhoneResolutionTests
{
    [Fact]
    public void GetOrderedMemberPhones_WithAllPhones_ReturnsHomeCellWork()
    {
        var @event = new TcnInformationEvent
        {
            MemberHomePhone = "555-111-1111",
            MemberCellPhone = "555-222-2222",
            MemberWorkPhone = "555-333-3333"
        };

        var (home, cell, work) = TcnOralNotificationService.GetOrderedMemberPhones(@event);

        Assert.Equal("555-111-1111", home);
        Assert.Equal("555-222-2222", cell);
        Assert.Equal("555-333-3333", work);
    }

    [Fact]
    public void GetOrderedMemberPhones_WithNoPhones_ReturnsAllNull()
    {
        var @event = new TcnInformationEvent
        {
            MemberHomePhone = null,
            MemberCellPhone = "",
            MemberWorkPhone = "  "
        };

        var (home, cell, work) = TcnOralNotificationService.GetOrderedMemberPhones(@event);

        Assert.Null(home);
        Assert.Null(cell);
        Assert.Null(work);
    }

    [Fact]
    public void GetOrderedMemberPhones_WithDuplicates_RemovesDuplicates()
    {
        var @event = new TcnInformationEvent
        {
            MemberHomePhone = "555-1234",
            MemberCellPhone = "555-1234",
            MemberWorkPhone = "555-5678"
        };

        var (home, cell, work) = TcnOralNotificationService.GetOrderedMemberPhones(@event);

        Assert.Equal("555-1234", home);
        Assert.Equal("555-5678", cell);
        Assert.Null(work);
    }

    [Fact]
    public void GetOrderedMemberPhones_WithAllDuplicates_ReturnsSinglePhone()
    {
        var @event = new TcnInformationEvent
        {
            MemberHomePhone = "555-1234",
            MemberCellPhone = "555-1234",
            MemberWorkPhone = "555-1234"
        };

        var (home, cell, work) = TcnOralNotificationService.GetOrderedMemberPhones(@event);

        Assert.Equal("555-1234", home);
        Assert.Null(cell);
        Assert.Null(work);
    }

    [Fact]
    public void GetOrderedMemberPhones_WithInvalidEntries_SkipsInvalid()
    {
        var @event = new TcnInformationEvent
        {
            MemberHomePhone = null,
            MemberCellPhone = "  ",
            MemberWorkPhone = "555-333-3333"
        };

        var (home, cell, work) = TcnOralNotificationService.GetOrderedMemberPhones(@event);

        Assert.Equal("555-333-3333", home);
        Assert.Null(cell);
        Assert.Null(work);
    }

    [Fact]
    public void GetOrderedMemberPhones_WithWhitespacePadding_TrimsValues()
    {
        var @event = new TcnInformationEvent
        {
            MemberHomePhone = "  555-111-1111  ",
            MemberCellPhone = "555-222-2222"
        };

        var (home, cell, work) = TcnOralNotificationService.GetOrderedMemberPhones(@event);

        Assert.Equal("555-111-1111", home);
        Assert.Equal("555-222-2222", cell);
        Assert.Null(work);
    }
}
