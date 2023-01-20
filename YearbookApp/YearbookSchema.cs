using System;
using GraphQL.Instrumentation;
using GraphQL.Types;

namespace YearbookApp
{
    public class YearbookSchema : Schema
    {
        public YearbookSchema(IServiceProvider provider) : base(provider)
        {
            Query = (YearbookQuery)provider.GetService(typeof(YearbookQuery)) ?? throw new InvalidOperationException();
            FieldMiddleware.Use(new InstrumentFieldsMiddleware());
        }
    }
}