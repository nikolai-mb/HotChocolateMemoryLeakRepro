namespace HCMemoryLeakRepro;

public sealed class CarBrand
{
    public CarBrand(int id, string name)
    {
        Id = id;
        Name = name;
    }
    
#pragma warning disable CS8618
    private CarBrand() { }
#pragma warning restore CS8618
    
    public int Id { get; private set; }
    public string Name { get; private set; }
}