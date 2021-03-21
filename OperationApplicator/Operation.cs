﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace OperationApplicator
{
    public record Operation
    {
        public OperationTypes OperationType { get; init; }
        public OperationPropertyPath PropertyPath { get; init; }
        public object Value { get; init; }

        public static Operation Create<TSource, TProperty>(TSource source, Expression<Func<TSource, TProperty>> property) where TSource : class
        {
            var properties = new LinkedList<PropertyInfo>();
            var memberExp = property.Body as MemberExpression ?? (property.Body as UnaryExpression)?.Operand as MemberExpression;
            while (memberExp is object)
            {
                properties.AddFirst((PropertyInfo)memberExp.Member);
                memberExp = memberExp.Expression as MemberExpression;
            }

            static (OperationPropertyPath path, object value) getPathAndValue(IEnumerator<PropertyInfo> pathParts, object value)
            {
                if (pathParts.MoveNext())
                {
                    var currentPath = pathParts.Current;
                    var (p, v) = getPathAndValue(
                        pathParts,
                        value is null
                            ? (currentPath.PropertyType.IsValueType ? Activator.CreateInstance(currentPath.PropertyType) : null)
                            : currentPath.GetValue(value));

                    return (new OperationPropertyPath { Property = currentPath, Next = p }, v);
                }
                else
                {
                    return (null, value);
                }
            }

            var (path, value) = getPathAndValue(properties.GetEnumerator(), source);
            return new Operation
            {
                OperationType = OperationTypes.replace,
                PropertyPath = path,
                Value = value
            };
        }
    }
}
