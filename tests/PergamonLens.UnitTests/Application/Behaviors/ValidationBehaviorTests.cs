using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using PergamonLens.Application.Common.Behaviors;
using ValidationException = PergamonLens.Application.Common.Exceptions.ValidationException;

namespace PergamonLens.UnitTests.Application.Behaviors;

public class ValidationBehaviorTests
{
    public sealed record DummyRequest(string Name) : IRequest<string>;

    public sealed class DummyValidator : AbstractValidator<DummyRequest>
    {
        public DummyValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        }
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ShouldThrowValidationExceptionAndNotCallNext()
    {
        // Arrange
        var request = new DummyRequest("");
        var validators = new List<IValidator<DummyRequest>> { new DummyValidator() };
        var behavior = new ValidationBehavior<DummyRequest, string>(validators);

        bool nextCalled = false;
        Task<string> Next(CancellationToken ct)
        {
            nextCalled = true;
            return Task.FromResult("Success");
        }

        // Act
        var act = () => behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("Name");
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_ShouldCallNextAndReturnResponse()
    {
        // Arrange
        var request = new DummyRequest("ValidName");
        var validators = new List<IValidator<DummyRequest>> { new DummyValidator() };
        var behavior = new ValidationBehavior<DummyRequest, string>(validators);

        bool nextCalled = false;
        Task<string> Next(CancellationToken ct)
        {
            nextCalled = true;
            return Task.FromResult("Success");
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        result.Should().Be("Success");
        nextCalled.Should().BeTrue();
    }
}

