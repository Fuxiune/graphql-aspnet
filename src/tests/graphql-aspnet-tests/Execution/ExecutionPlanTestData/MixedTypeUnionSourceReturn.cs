﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************

namespace GraphQL.AspNet.Tests.Execution.ExecutionPlanTestData
{
    using System;
    using GraphQL.AspNet.Schemas.TypeSystem;

    public class MixedTypeUnionSourceReturn : GraphUnionProxy
    {
        public static int TotalCallCount = 0;

        public MixedTypeUnionSourceReturn()
            : base(typeof(MixedReturnTypeA), typeof(MixedReturnTypeB))
        {
        }

        public override Type MapType(Type runtimeObjectType)
        {
            TotalCallCount += 1;
            return runtimeObjectType;
        }
    }
}