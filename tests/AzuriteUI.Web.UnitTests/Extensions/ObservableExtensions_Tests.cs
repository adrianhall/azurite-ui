using System.Reactive.Linq;
using AzuriteUI.Web.Extensions;
using AzuriteUI.Web.UnitTests.Helpers;

namespace AzuriteUI.Web.UnitTests.Extensions;

[ExcludeFromCodeCoverage]
public class ObservableExtensions_Tests
{
    #region ToObservable Tests

    [Fact(Timeout = 15000)]
    public void ToObservable_WithNullSource_ShouldThrowArgumentNullException()
    {
        // Arrange
        IAsyncEnumerable<int>? source = null;

        // Act
        Action act = () => source!.ToObservable();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(Timeout = 15000)]
    public async Task ToObservable_WithEmptyAsyncEnumerable_ShouldCompleteWithoutEmittingItems()
    {
        // Arrange
        var source = Utils.CreateAsyncEnumerable<int>([]);

        // Act
        var result = await source.ToObservable().ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task ToObservable_WithSingleItem_ShouldEmitItemAndComplete()
    {
        // Arrange
        var source = Utils.CreateAsyncEnumerable([42]);

        // Act
        var result = await source.ToObservable().ToList();

        // Assert
        result.Should().ContainSingle().Which.Should().Be(42);
    }

    [Fact(Timeout = 15000)]
    public async Task ToObservable_WithMultipleItems_ShouldEmitAllItemsInOrder()
    {
        // Arrange
        var source = Utils.CreateAsyncEnumerable([1, 2, 3, 4, 5]);

        // Act
        var result = await source.ToObservable().ToList();

        // Assert
        result.Should().HaveCount(5);
        result.Should().Equal(1, 2, 3, 4, 5);
    }

    [Fact(Timeout = 15000)]
    public async Task ToObservable_WithException_ShouldThrowException()
    {
        // Arrange
        var source = Utils.CreateAsyncEnumerableWithException<int>(new InvalidOperationException("Test exception"));

        // Act
        Func<Task> act = async () => await source.ToObservable().ToList();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    [Fact(Timeout = 15000)]
    public async Task ToObservable_WithLargeSequence_ShouldEmitAllItems()
    {
        // Arrange
        var source = Utils.CreateAsyncEnumerable(Enumerable.Range(1, 100).ToArray());

        // Act
        var result = await source.ToObservable().ToList();

        // Assert
        result.Should().HaveCount(100);
        result.Should().BeInAscendingOrder();
    }

    [Fact(Timeout = 15000)]
    public async Task ToObservable_WithDifferentType_ShouldEmitCorrectType()
    {
        // Arrange
        var source = Utils.CreateAsyncEnumerable(["hello", "world", "test"]);

        // Act
        var result = await source.ToObservable().ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Equal("hello", "world", "test");
    }

    [Fact(Timeout = 15000)]
    public async Task ToObservable_WithExceptionAfterItems_ShouldThrowException()
    {
        // Arrange
        var source = Utils.CreateAsyncEnumerableWithExceptionAfterItems(3, new InvalidOperationException("Test exception"));

        // Act
        Func<Task> act = async () => await source.ToObservable().ToList();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    #endregion
}
