using AngleSharp.Dom;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;

#pragma warning disable CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.

namespace AzuriteUI.Web.IntegrationTests.Helpers;

/// <summary>
/// Hooks for enabling the introduction of <see cref="ElementAssertions"/>.
/// </summary>
public static class ElementAssertionExtensions
{
    /// <summary>
    /// Hook to enable the ElementAssertions on an <see cref="IElement"/> instance.
    /// </summary>
    /// <param name="element">The <see cref="IElement"/> instance under test.</param>
    /// <returns>The assertions object.</returns>
    public static ElementAssertions Should(this IElement? element)
    {
        return new ElementAssertions(element, AssertionChain.GetOrCreate());
    }
}

/// <summary>
/// A set of AwesomeAssertions for <see cref="IElement"/> instances.
/// </summary>
/// <param name="element">The <see cref="IElement"/> under test.</param>
/// <param name="chain">The assertion chain.</param>
public class ElementAssertions(IElement? element, AssertionChain chain) : ReferenceTypeAssertions<IElement?, ElementAssertions>(element, chain)
{
    /// <summary>
    /// The identifier for the assertions subject.
    /// </summary>
    protected override string Identifier => nameof(IElement);

    /// <summary>
    /// Asserts that the element has the specified attribute with the expected value.
    /// </summary>
    /// <param name="attributeName">The name of the attribute that should exist.</param>
    /// <param name="expectedValue">The expected value of the attribute.</param>
    /// <param name="because">A formatted phrase as is supported by <see cref="string.Format(string, object[])"/> explaining why the assertion is needed. If the phrase does not start with the word "because", it is prepended automatically.</param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because"/>.</param>
    /// <returns>The current <see cref="ElementAssertions"/> instance for chaining.</returns>
    [CustomAssertion]
    public AndConstraint<ElementAssertions> HaveAttribute(string attributeName, string expectedValue, string because = "", params object[] becauseArgs)
    {
        chain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject is not null)
            .FailWith("Expected element to have attribute {0} with value {1}{reason}, but found <null>.", attributeName, expectedValue)
        .Then
            .ForCondition(Subject!.GetAttribute(attributeName) == expectedValue)
            .FailWith("Expected element to have attribute {0} with value {1}{reason}, but found {2}.", attributeName, expectedValue, Subject.GetAttribute(attributeName));

        return new AndConstraint<ElementAssertions>(this);
    }

    /// <summary>
    /// Asserts that the element has the specified text content.
    /// </summary>
    /// <param name="textContent">The text content to check for.</param>
    /// <param name="because">A formatted phrase as is supported by <see cref="string.Format(string, object[])"/> explaining why the assertion is needed. If the phrase does not start with the word "because", it is prepended automatically.</param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because"/>.</param>
    /// <returns>The current <see cref="ElementAssertions"/> instance for chaining.</returns>
    [CustomAssertion]
    public AndConstraint<ElementAssertions> HaveTextContent(string textContent, string because = "", params object[] becauseArgs)
    {
        chain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject is not null)
            .FailWith("Expected element to have text content {0}{reason}, but found <null>.", textContent)
        .Then
            .ForCondition(Subject!.TextContent?.Trim() == textContent)
            .FailWith("Expected element to have text content {0}{reason}, but found {1}.", textContent, Subject.TextContent);

        return new AndConstraint<ElementAssertions>(this);
    }

    /// <summary>
    /// Asserts that the element has the specified text content.
    /// </summary>
    /// <param name="textContent">The text content to check for.</param>
    /// <param name="because">A formatted phrase as is supported by <see cref="string.Format(string, object[])"/> explaining why the assertion is needed. If the phrase does not start with the word "because", it is prepended automatically.</param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because"/>.</param>
    /// <returns>The current <see cref="ElementAssertions"/> instance for chaining.</returns>
    [CustomAssertion]
    public AndConstraint<ElementAssertions> NotHaveTextContent(string textContent, string because = "", params object[] becauseArgs)
    {
        chain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject is not null)
            .FailWith("Expected element to have text content {0}{reason}, but found <null>.", textContent)
        .Then
            .ForCondition(Subject!.TextContent?.Trim() != textContent)
            .FailWith("Expected element to not have text content {0}{reason}, but found {1}.", textContent, Subject.TextContent);

        return new AndConstraint<ElementAssertions>(this);
    }
}