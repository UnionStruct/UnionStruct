using ExampleAuth.Api.Domain;
using MediatR;

namespace ExampleAuth.Api.Behaviors;

public class ValidationBehavior<T1, T2> : IPipelineBehavior<T1, T2>
    where T1 : IValidatable
{
    public Task<T2> Handle(T1 request, RequestHandlerDelegate<T2> next, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}