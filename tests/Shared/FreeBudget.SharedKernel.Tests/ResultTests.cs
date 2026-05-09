using FluentAssertions;
using FreeBudget.SharedKernel.Results;

namespace FreeBudget.SharedKernel.Tests;

public class ResultTests
{
    [Fact]
    public void Success_result_contains_value_and_no_error()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(42);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_result_contains_error_and_no_value()
    {
        var result = Result<int>.Failure("something went wrong");

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("something went wrong");
        result.Value.Should().Be(default(int));
    }
}
