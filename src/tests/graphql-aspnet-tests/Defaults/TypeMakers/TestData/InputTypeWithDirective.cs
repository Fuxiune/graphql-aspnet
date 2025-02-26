﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************
namespace GraphQL.AspNet.Tests.Defaults.TypeMakers.TestData
{
    using GraphQL.AspNet.Attributes;

    [ApplyDirective(typeof(DirectiveWithArgs), 44, "input arg")]
    public class InputTypeWithDirective
    {
        public string Prop1 { get; set; }
    }
}