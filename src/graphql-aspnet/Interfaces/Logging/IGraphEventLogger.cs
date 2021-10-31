﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************

namespace GraphQL.AspNet.Interfaces.Logging
{
    using System;
    using System.Security.Claims;
    using GraphQL.AspNet.Execution.Contexts;
    using GraphQL.AspNet.Execution.InputModel;
    using GraphQL.AspNet.Interfaces.Execution;
    using GraphQL.AspNet.Interfaces.Middleware;
    using GraphQL.AspNet.Interfaces.TypeSystem;
    using GraphQL.AspNet.Internal.Interfaces;
    using GraphQL.AspNet.Middleware.FieldAuthorization;
    using GraphQL.AspNet.Middleware.FieldExecution;
    using GraphQL.AspNet.Middleware.QueryExecution;

    /// <summary>
    /// A logging interface describing specific logged events in the completion of a graphql request.
    /// </summary>
    public interface IGraphEventLogger : IGraphLogger
    {
        /// <summary>
        /// Recorded when the startup services generates a new schema instance.
        /// </summary>
        /// <typeparam name="TSchema">The type of the schema that was generated.</typeparam>
        /// <param name="schema">The schema instance.</param>
        void SchemaInstanceCreated<TSchema>(TSchema schema)
            where TSchema : class, ISchema;

        /// <summary>
        /// Recorded when the startup services generate a new pipeline for the target schema.
        /// </summary>
        /// <typeparam name="TSchema">The type of the schema for which the pipeline was generated.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        void SchemaPipelineRegistered<TSchema>(ISchemaPipeline pipeline)
            where TSchema : class, ISchema;

        /// <summary>
        /// Recorded when the startup services registers a publically available ASP.NET MVC route to which
        /// end users can submit graphql queries.
        /// </summary>
        /// <typeparam name="TSchema">The type of the schema the route was registered for.</typeparam>
        /// <param name="routePath">The relative route path (e.g. '/graphql').</param>
        void SchemaRouteRegistered<TSchema>(string routePath)
            where TSchema : class, ISchema;

        /// <summary>
        /// Recorded when a new request is generated by a query controller and passed to an
        /// executor for processing. This event is recorded before any action is taken.
        /// </summary>
        /// <param name="queryContext">The query context.</param>
        void RequestReceived(GraphQueryExecutionContext queryContext);

        /// <summary>
        /// Recorded when an executor attempts to fetch a query plan from its local cache but failed
        /// either because the the plan is not cached or a retrieval operation failed.
        /// </summary>
        /// <typeparam name="TSchema">The type of the schema for which the query hash was generated.</typeparam>
        /// <param name="key">The key that was searched for in the cache.</param>
        void QueryPlanCacheFetchMiss<TSchema>(string key)
            where TSchema : class, ISchema;

        /// <summary>
        /// Recorded when the security middleware invokes a security challenge
        /// against a <see cref="ClaimsPrincipal" />.
        /// </summary>
        /// <param name="context">The authorization context that contains the request to be authorized.</param>
        void FieldResolutionSecurityChallenge(GraphFieldAuthorizationContext context);

        /// <summary>
        /// Recorded when the security middleware completes a security challenge and renders a
        /// result.
        /// </summary>
        /// <param name="context">The authorization context that completed authorization.</param>
        void FieldResolutionSecurityChallengeResult(GraphFieldAuthorizationContext context);

        /// <summary>
        /// Recorded when an executor attempts, and succeeds, to retrieve a query plan from its local cache.
        /// </summary>
        /// <typeparam name="TSchema">The type of the schema for which the query hash was generated.</typeparam>
        /// <param name="key">The key that was searched for in the cache.</param>
        void QueryPlanCacheFetchHit<TSchema>(string key)
            where TSchema : class, ISchema;

        /// <summary>
        /// Recorded when an executor successfully caches a newly created query plan to its
        /// local cache for future use.
        /// </summary>
        /// <param name="key">The key the plan is to be cached under.</param>
        /// <param name="queryPlan">The completed plan that was cached.</param>
        void QueryPlanCached(string key, IGraphQueryPlan queryPlan);

        /// <summary>
        /// Recorded when an executor finishes creating a query plan and is ready to
        /// cache and execute against it.
        /// </summary>
        /// <param name="queryPlan">The generated query plan.</param>
        void QueryPlanGenerated(IGraphQueryPlan queryPlan);

        /// <summary>
        /// Recorded by a field resolver when it starts resolving a field context and
        /// set of source items given to it. This occurs prior to the middleware pipeline being executed.
        /// </summary>
        /// <param name="context">The field resolution context that is being completed.</param>
        void FieldResolutionStarted(FieldResolutionContext context);

        /// <summary>
        /// Recorded by a field resolver when it completes resolving a field context (and its children).
        /// This occurs after the middleware pipeline is executed.
        /// </summary>
        /// <param name="context">The context of the field resolution that was completed.</param>
        void FieldResolutionCompleted(FieldResolutionContext context);

        /// <summary>
        /// Recorded when a controller begins the invocation of an action method to resolve
        /// a field request.
        /// </summary>
        /// <param name="action">The action method on the controller being invoked.</param>
        /// <param name="request">The request being completed by the action method.</param>
        void ActionMethodInvocationRequestStarted(IGraphMethod action, IDataRequest request);

        /// <summary>
        /// Recorded when a controller completes validation of the model data that will be passed
        /// to the action method.
        /// </summary>
        /// <param name="action">The action method on the controller being invoked.</param>
        /// <param name="request">The request being completed by the action method.</param>
        /// <param name="modelState">The model data that was validated.</param>
        void ActionMethodModelStateValidated(IGraphMethod action, IDataRequest request, InputModelStateDictionary modelState);

        /// <summary>
        /// Recorded after a controller invokes and receives a result from an action method.
        /// </summary>
        /// <param name="action">The action method on the controller being invoked.</param>
        /// <param name="request">The request being completed by the action method.</param>
        /// <param name="result">The result object that was returned from the action method.</param>
        void ActionMethodInvocationCompleted(IGraphMethod action, IDataRequest request, object result);

        /// <summary>
        /// Recorded when the invocation of action method generated a known exception; generally
        /// related to target invocation errors.
        /// </summary>
        /// <param name="action">The action method on the controller being invoked.</param>
        /// <param name="request">The request being completed by the action method.</param>
        /// <param name="exception">The exception that was generated.</param>
        void ActionMethodInvocationException(IGraphMethod action, IDataRequest request, Exception exception);

        /// <summary>
        /// Recorded when the invocation of action method generated an unknown exception. This
        /// event is called when custom resolver code throws an unhandled exception.
        /// </summary>
        /// <param name="action">The action method on the controller being invoked.</param>
        /// <param name="request">The request being completed by the action method.</param>
        /// <param name="exception">The exception that was generated.</param>
        void ActionMethodUnhandledException(IGraphMethod action, IDataRequest request, Exception exception);

        /// <summary>
        /// Recorded by an executor after the entire graphql operation has been completed
        /// and final results have been generated.
        /// </summary>
        /// <param name="queryContext">The query context.</param>
        void RequestCompleted(GraphQueryExecutionContext queryContext);
    }
}