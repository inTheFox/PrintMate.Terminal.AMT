namespace ProjectParserTest.Parsers.Shared.Models
{
    public class Part
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Data Data { get; set; } = new Data();
    }
}
