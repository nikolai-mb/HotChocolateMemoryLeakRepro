using System.Text;
using System.Threading.Channels;
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
			? "id: { in: [ 1 ] }"
			: "id: { eq: 1 }";

		var query = $$"""{ "query": "{ carBrands(where:{ {{filter}} }) { id name } }" }""";

		var channel = Channel.CreateBounded<int>(1);

		List<Task> consumerTasks = new();
		for (var i = 0; i < 4; i++)
		{
			consumerTasks.Add(Task.Run(async () =>
			{
				while (await channel.Reader.WaitToReadAsync(ct) && channel.Reader.TryRead(out _))
				{
					try
					{
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
					catch
					{
						// Errors (such as timeouts) are ignored
					}
				}
			}, ct));
		}
		
		for (var i = 0; i < 100_000; i++)
		{
			if (i % 5000 == 0) Console.WriteLine($"Completed {i}");
			await channel.Writer.WriteAsync(i, ct);
		}
		
		channel.Writer.Complete();

		await Task.WhenAll(consumerTasks);
		return "All requests finished.";
	}
}