namespace BlazorApp1;
public partial class swaggerClient
{
    [ActivatorUtilitiesConstructor]
    public swaggerClient(HttpClient httpClient) : this(string.Empty, httpClient)
    {
        
    }
}
