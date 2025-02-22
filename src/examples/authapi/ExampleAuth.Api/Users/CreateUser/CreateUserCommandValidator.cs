using FluentValidation;

namespace ExampleAuth.Api.Users.CreateUser;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(c => c.Login).NotEmpty().OverridePropertyName("login");
        RuleFor(c => c.Password).NotEmpty().OverridePropertyName("password");
    }
}