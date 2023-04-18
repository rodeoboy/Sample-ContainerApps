namespace Sample.Worker.StateMachines;

using MassTransit;


public class OrderStateDefinition :
    SagaDefinition<OrderState>
{
    protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<OrderState> sagaConfigurator)
    {
        endpointConfigurator.UseMessageRetry(r => r.Intervals(100, 200, 500, 1000));
        endpointConfigurator.UseInMemoryOutbox();
    }
}