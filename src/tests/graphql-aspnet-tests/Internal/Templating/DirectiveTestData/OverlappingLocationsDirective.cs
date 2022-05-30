﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************
namespace GraphQL.AspNet.Tests.Internal.Templating.DirectiveTestData
{
    using System.Threading.Tasks;
    using GraphQL.AspNet.Attributes;
    using GraphQL.AspNet.Directives;
    using GraphQL.AspNet.Interfaces.Controllers;
    using GraphQL.AspNet.Schemas.TypeSystem;

    public class OverlappingLocationsDirective : GraphDirective
    {
        [DirectiveLocations(DirectiveLocation.FIELD)]
        [DirectiveLocations(DirectiveLocation.FIELD | DirectiveLocation.MUTATION)]
        public Task<IGraphActionResult> Execute(int arg1, string arg2)
        {
            return null;
        }
    }
}