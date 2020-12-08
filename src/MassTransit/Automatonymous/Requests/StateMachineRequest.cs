namespace Automatonymous.Requests
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Events;
    using GreenPipes.Internals.Reflection;
    using MassTransit;
    using MassTransit.Internals.Extensions;


    public class StateMachineRequest<TInstance, TRequest, TResponse> :
        Request<TInstance, TRequest, TResponse>
        where TInstance : class, SagaStateMachineInstance
        where TRequest : class
        where TResponse : class
    {
        readonly IList<string> _accept;

        readonly string _name;
        readonly ReadWriteProperty<TInstance, Guid?> _requestIdProperty;
        readonly RequestSettings _settings;

        public StateMachineRequest(string name, Expression<Func<TInstance, Guid?>> requestIdExpression, RequestSettings settings)
        {
            _name = name;
            _settings = settings;

            _accept = new List<string>();

            AcceptResponse<TResponse>();

            _requestIdProperty = new ReadWriteProperty<TInstance, Guid?>(requestIdExpression.GetPropertyInfo());
        }

        string Request<TInstance, TRequest, TResponse>.Name => _name;
        RequestSettings Request<TInstance, TRequest, TResponse>.Settings => _settings;
        public Event<TResponse> Completed { get; set; }
        public Event<Fault<TRequest>> Faulted { get; set; }
        public Event<RequestTimeoutExpired<TRequest>> TimeoutExpired { get; set; }
        public State Pending { get; set; }

        public void SetRequestId(TInstance instance, Guid? requestId)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            _requestIdProperty.Set(instance, requestId);
        }

        public Guid? GetRequestId(TInstance instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            return _requestIdProperty.Get(instance);
        }

        public void SetSendContextHeaders(SendContext<TRequest> context)
        {
            context.Headers.Set(MessageHeaders.Request.Accept, _accept);
        }

        public bool EventFilter(EventContext<TInstance, RequestTimeoutExpired<TRequest>> context)
        {
            if (!context.TryGetPayload(out ConsumeContext<RequestTimeoutExpired<TRequest>> consumeContext))
                return false;

            if (!consumeContext.RequestId.HasValue)
                return false;

            Guid? requestId = _requestIdProperty.Get(context.Instance);

            return requestId.HasValue && requestId.Value == consumeContext.RequestId.Value;
        }

        protected void AcceptResponse<T>()
            where T : class
        {
            _accept.Add(MessageUrn.ForTypeString<T>());
        }
    }


    public class StateMachineRequest<TInstance, TRequest, TResponse, TResponse2> :
        StateMachineRequest<TInstance, TRequest, TResponse>,
        Request<TInstance, TRequest, TResponse, TResponse2>
        where TInstance : class, SagaStateMachineInstance
        where TRequest : class
        where TResponse : class
        where TResponse2 : class
    {
        public StateMachineRequest(string name, Expression<Func<TInstance, Guid?>> requestIdExpression, RequestSettings settings)
            : base(name, requestIdExpression, settings)
        {
            AcceptResponse<TResponse2>();
        }

        public Event<TResponse2> Completed2 { get; set; }
    }
}
