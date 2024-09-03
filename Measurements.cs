namespace mg_1brc;
public record Measurements 
{
    public double Min {get;set;}
    public double Max {get;set;}
    public double Total {get;set;}
    public int Count {get;set;}
    public double Avg => Math.Round(Total / Count, 1);

    public void Add(double value)
    {
        Total += value;
        Count++;
        Min = Math.Min(Min, value);
        Max = Math.Max(Max, value);
    }

    public void Reconcile(Measurements m)
    {
        Total += m.Total;
        Count += m.Count;
        Min = Math.Min(Min, m.Min);
        Max = Math.Max(Max, m.Max); 
    }
}

