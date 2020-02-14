namespace ResilienceDecorators.Tests
{
    internal class Record
    {
        internal int Id { get; set; }

        internal string Name { get; set; }

        internal static Record SampleRecord =>
            new Record
            {
                Id = 1,
                Name = "Test"
            };
    }
}