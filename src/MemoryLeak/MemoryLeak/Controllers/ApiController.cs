using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryLeak.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        public ApiController()
        {
            Interlocked.Increment(ref DiagnosticsController.Requests);
        }

        private static ConcurrentBag<string> _staticStrings = new ConcurrentBag<string>();

        [HttpGet("staticstring")]
        public ActionResult<string> GetStaticString()
        {
            var bigString = new String('x', 10 * 1024);
            _staticStrings.Add(bigString);
            return _staticStrings.Count.ToString(); //bigString;
        }

        [HttpGet("bigstring")]
        public ActionResult<string> GetBigString()
        {
            return new String('x', 10 * 1024);
        }

        [HttpGet("loh/{size=85000}")]
        public int GetLOH(int size)
        {
            return new byte[size].Length;
        }

        private static readonly string TempPath = Path.GetTempPath();

        [HttpGet("fileprovider")]
        public void GetFileProvider()
        {
            var fp = new PhysicalFileProvider(TempPath);
            fp.Watch("*.*");
        }

        [HttpGet("httpclient1")]
        public async Task<int> GetHttpClient1(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var result = await httpClient.GetAsync(url);
                return (int)result.StatusCode;
            }
        }

        private static readonly HttpClient _httpClient = new HttpClient();

        [HttpGet("httpclient2")]
        public async Task<int> GetHttpClient2(string url)
        {
            var result = await _httpClient.GetAsync(url);
            return (int)result.StatusCode;
        }

        [HttpGet("array/{size}")]
        public byte[] GetArray(int size)
        {
            var array = new byte[size];

            var random = new Random();
            random.NextBytes(array);

            return array;
        }

        private static ArrayPool<byte> _arrayPool = ArrayPool<byte>.Create();

        private class PooledArray : IDisposable
        {
            public byte[] Array { get; private set; }

            public PooledArray(int size)
            {
                Array = _arrayPool.Rent(size);
            }

            public void Dispose()
            {
                _arrayPool.Return(Array);
            }
        }

        [HttpGet("pooledarray/{size}")]
        public byte[] GetPooledArray(int size)
        {
            var pooledArray = new PooledArray(size);

            var random = new Random();
            random.NextBytes(pooledArray.Array);

            HttpContext.Response.RegisterForDispose(pooledArray);

            return pooledArray.Array;
        }

        [HttpGet("json/{size}")]
        public string GetJson(int size)
        {
            var array = Enumerable.Range(0, size)
                .Select(i => new MyDto())
                .ToArray();

            var json = JsonSerializer.Serialize(array);

            return json;
        }

        [HttpGet("jsonsourcegenerated/{size}")]
        public string GetSourceGeneratedJson(int size)
        {
            var array = Enumerable.Range(0, size)
                .Select(i => new MyDto())
                .ToArray();

            var json = JsonSerializer.Serialize(array, typeof(MyDto[]), MyJsonContext.Default);

            return json;
        }

        [HttpGet("bigjson")]
        public async Task<IEnumerable<MyDto>> GetBigJson()
        {
            using var fileStream = System.IO.File.OpenRead("./bigjson.json");

            var data = await JsonSerializer.DeserializeAsync<MyDto[]>(fileStream);

            return data;
        }

        [HttpGet("bigjsonsyncstream")]
        public async Task<IEnumerable<MyDto>> GetBigJsonSyncStream()
        {
            // cannot use using(), because it will result in ObjectDisposedException
            var fileStream = System.IO.File.OpenRead("./bigjson.json");

            // Instead registering fileStream for disposal after request is fully processed
            HttpContext.Response.RegisterForDispose(fileStream);

            var jsonEnumerable = await JsonSerializer.DeserializeAsync<IEnumerable<MyDto>>(fileStream);

            return jsonEnumerable;
        }

        [HttpGet("bigjsonstream")]
        public IAsyncEnumerable<MyDto> GetBigJsonStream()
        {
            // cannot use using(), because it will result in ObjectDisposedException
            var fileStream = System.IO.File.OpenRead("./bigjson.json");

            // Instead registering fileStream for disposal after request is fully processed
            HttpContext.Response.RegisterForDispose(fileStream);

            var jsonAsyncEnumerable = JsonSerializer.DeserializeAsyncEnumerable<MyDto>(fileStream);

            return jsonAsyncEnumerable;
        }

    }

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

    [JsonSerializable(typeof(MyDto[]))]
    internal partial class MyJsonContext : JsonSerializerContext
    {
    }
}
