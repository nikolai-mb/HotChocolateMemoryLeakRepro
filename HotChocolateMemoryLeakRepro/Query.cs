using System.Diagnostics;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace HCMemoryLeakRepro;

public class Query
{
    [UseDbContext(typeof(ApplicationDbContext))]
    [UseFiltering]
    public IQueryable<CarBrand> CarBrands(ApplicationDbContext dbContext)
    {
        return dbContext.CarBrands.AsNoTracking();
    }

	public async Task<string> SimulateLoadAsync(bool createMemoryLeak, CancellationToken ct)
	{
		HttpClient client = new(); // Socket exhaustion is not a concern here

		var filter = createMemoryLeak
			? "in: [ 1 ]"
			: "eq: 1";
		
		var query = $$"""{ "query": "{ carBrands(where:{ id: { {{filter}} } }) { id name } }" }""";

		var start = Stopwatch.GetTimestamp();
		var batchStart = start;
		
		for (var i = 1; i < 100_001; i++)
		{
			if (i % 5000 == 0)
			{
				Console.WriteLine($"Batch complete in {double.Round(Stopwatch.GetElapsedTime(batchStart).TotalSeconds, 1)} seconds ({i} / 100000)");
				batchStart = Stopwatch.GetTimestamp();
			}
			
			HttpRequestMessage request = new()
			{
				RequestUri = new Uri("http://localhost:4161/graphql"),
				Method = HttpMethod.Post,
			};

			request.Content = new StringContent(query, Encoding.UTF8, "application/json");

			using var response = await client.SendAsync(request, ct);

			if (!response.IsSuccessStatusCode)
			{
				Console.WriteLine(await response.Content.ReadAsStringAsync(ct));
			}
			
			response.EnsureSuccessStatusCode();
		}

		var result = $"All requests finished in {double.Round(Stopwatch.GetElapsedTime(start).TotalSeconds, 1)} seconds.";
		Console.WriteLine(result);
		return result;
	}
}