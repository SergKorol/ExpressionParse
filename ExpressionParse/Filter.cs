using System.Text.Json;
using BenchmarkDotNet.Attributes;

namespace ExpressionParse;

public class Filter
{
    [Benchmark]
    public void LinqFiltering()
    {
        var json = File.ReadAllText("data.json");
        
        var people = JsonSerializer.Deserialize<List<Person>>(json);

        if (people == null) return;
        foreach (var person in people.Where(x => x.Age > 52))
        {
            Console.WriteLine(JsonSerializer.Serialize(person));
        }
        Console.WriteLine();
    }

    [Benchmark]
    public void ExpressionFiltering()
    {
        var query = File.ReadAllText("query.json");
        var queryDocument = JsonDocument.Parse(query);
        var expression = QueryParser.Parse(queryDocument);
        
        var documentsRaw = File.ReadAllText("data.json");
        var serializerOptions = new JsonSerializerOptions();
        serializerOptions.Converters.Add(new DictionaryStringObjectJsonConverter());
        var documents = JsonSerializer.Deserialize<IEnumerable
            <IReadOnlyDictionary<string, object>>>(documentsRaw,
            serializerOptions)!;
        var filtered = documents.AsQueryable().Where(expression);
        foreach (var document in filtered)
        {
            // string result = null; 
            // foreach (var item in document)
            // {
            //
            //     switch (item.Key)
            //     {
            //         case "FirstName":
            //             result = $"Name: {item.Value}";
            //             break;
            //         case "LastName":
            //             result += $" {item.Value},";
            //             break;
            //         case "Age":
            //             result += $" Age: {item.Value}";
            //             break;
            //     }
            // }
            Console.WriteLine(JsonSerializer.Serialize(document));
        }
        Console.WriteLine();
    }
}