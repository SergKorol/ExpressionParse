using System.Linq.Expressions;
using System.Text.Json;

namespace ExpressionParse;

public static class QueryParser
{
    private static readonly ParameterExpression
        DictionaryParameter = Expression.Parameter(typeof
            (IReadOnlyDictionary<string, object>), "input");
    
    static Expression GetLeftValueExpression(JsonProperty
        parentProperty, JsonProperty property)
    {
        var keyParam =
            Expression.Constant(parentProperty.Name);
        var indexer = typeof(IReadOnlyDictionary<string,
            object>).GetProperty("Item");
        var indexerExpr = Expression.Property(
            DictionaryParameter, indexer, keyParam);
        return property.Value.ValueKind switch
        {
            JsonValueKind.Number =>
                Expression.Unbox(indexerExpr, typeof(int)),
            JsonValueKind.String =>
                Expression.TypeAs(indexerExpr, typeof(string)),
            JsonValueKind.True or JsonValueKind.False =>
                Expression.TypeAs(indexerExpr, typeof(bool)),
            _ => indexerExpr
        };
    }
    
    static Expression GetRightValueExpression(JsonProperty
        property)
    {
        return property.Value.ValueKind switch
        {
            JsonValueKind.Number =>
                Expression.Constant(property.Value.GetInt32()),
            JsonValueKind.String => Expression.Constant(
                property.Value.GetString()),
            JsonValueKind.True or JsonValueKind.False =>
                Expression.Constant(property.Value
                    .GetBoolean()),
            _ => Expression.Empty()
        };
    }
    
    static Expression GetNestedFilterExpression(JsonProperty
        property)
    {
        Expression? currentExpression = null;
        foreach (var expressionProperty in
                 property.Value.EnumerateObject())
        {
            var getValueExpression = GetLeftValueExpression(
                property, expressionProperty);
            var valueConstantExpression =
                GetRightValueExpression(expressionProperty);
            Expression comparisonExpression =
                expressionProperty.Name switch
                {
                    "$lt" => Expression.LessThan(
                        getValueExpression, valueConstantExpression),
                    "$lte" => Expression.LessThanOrEqual(
                        getValueExpression, valueConstantExpression),
                    "$gt" => Expression.GreaterThan(
                        getValueExpression, valueConstantExpression),
                    "$gte" => Expression.GreaterThanOrEqual(
                        getValueExpression, valueConstantExpression),
                    _ => Expression.Empty()
                };
            if (currentExpression is not null)
            {
                currentExpression = Expression.And(
                    currentExpression, comparisonExpression);
            }
            else
            {
                currentExpression = comparisonExpression;
            }
        }
        return currentExpression ?? Expression.Empty();
    }
    
    static Expression GetFilterExpression(JsonProperty
        property)
    {
        return property.Value.ValueKind switch
        {
            JsonValueKind.Object =>
                GetNestedFilterExpression(property),
            _ => Expression.Equal(GetLeftValueExpression(
                property, property), GetRightValueExpression(
                property))
        };
    }
    
    static Expression GetOrExpression(Expression expression,
        JsonProperty property)
    {
        foreach (var element in property.Value.EnumerateArray())
        {
            var elementExpression = GetQueryExpression(element);
            expression = Expression.OrElse(expression,
                elementExpression);
        }
        return expression;
    }
    
    static Expression GetQueryExpression(JsonElement element)
    {
        Expression? currentExpression = null;
        foreach (var property in element.EnumerateObject())
        {
            Expression expression = property.Name switch
            {
                "$or" => GetOrExpression(currentExpression,
                    property),
                _ => GetFilterExpression(property)
            };
            if (currentExpression is not null && expression is
                    not BinaryExpression)
            {
                currentExpression = Expression.And(
                    currentExpression, expression);
            }
            else
            {
                currentExpression = expression;
            }
        }
        return currentExpression ?? Expression.Empty();
    }
    
    public static Expression<Func<IReadOnlyDictionary<string, object>,
        bool>> Parse(JsonDocument json)
    {
        var element = json.RootElement;
        var query = GetQueryExpression(element);
        return Expression.Lambda<Func<IReadOnlyDictionary<string,
            object>, bool>>(query, DictionaryParameter);
    }
}

