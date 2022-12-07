using System;
using System.Text.Json.Serialization;

namespace MemoryLeak.Models
{
    public class MyDto
    {
        public DateTimeOffset MyDate1 { get; set; }
        public DateTimeOffset MyDate2 { get; set; }
        public DateTimeOffset MyDate3 { get; set; }
        public int MyNumber1 { get; set; }
        public int MyNumber2 { get; set; }
        public int MyNumber3 { get; set; }
        public string MyString1 { get; set; }
        public string MyString2 { get; set; }
        public string MyString3 { get; set; }

        public MyDto()
        {
            this.MyDate1 = this.MyDate2 = this.MyDate3 = DateTimeOffset.Now;
            this.MyNumber1 = this.MyNumber2 = this.MyNumber3 = (int)DateTimeOffset.Now.Ticks;
            this.MyString1 = this.MyString2 = this.MyString3 = DateTimeOffset.Now.ToString();
        }
    }

    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
    [JsonSerializable(typeof(MyDto[]))]
    internal partial class MyJsonContext : JsonSerializerContext
    {
    }
}
