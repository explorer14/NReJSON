﻿using Newtonsoft.Json;
using NReJSON.IntegrationTests.TestTypes;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NReJSON.IntegrationTests
{
    public class DatabaseExtensionAsyncTests
    {
        public class JsonSetAsync : BaseIntegrationTest
        {
            [Fact]
            public async Task CanExecuteAsync()
            {
                var key = Guid.NewGuid().ToString("N");

                var result = await _db.JsonSetAsync(key, "{}");

                Assert.NotNull(result);
            }
        }

        public class JsonGetAsync : BaseIntegrationTest
        {
            [Fact]
            public async Task CanExecuteAsync()
            {
                var key = Guid.NewGuid().ToString("N");

                await _db.JsonSetAsync(key, "{\"hello\": \"world\", \"goodnight\": {\"value\": \"moon\"}}");

                var result = await _db.JsonGetAsync(key);

                Assert.False(result.IsNull);
            }

            [Fact]
            public async Task CanReturnDeserialisedObjectFromJsonAsync()
            {
                var customerToSave = new Customer
                {
                    Id = 12345,
                    Name = "XYZ Inc.",
                    RegisteredOn = DateTime.UtcNow,
                    CorporateAddress = new Address
                    {
                        City = "London",
                        Postcode = "NW1"
                    }
                };

                await _db.JsonSetAsync(
                    customerToSave.Id.ToString(),
                    JsonConvert.SerializeObject(customerToSave));

                var savedCustomer = await _db.JsonGetAsync<Customer>(
                    customerToSave.Id.ToString());
                Assert.True(savedCustomer.Equals(customerToSave));
            }
        }

        public class JsonDeleteAsync : BaseIntegrationTest
        {
            [Fact]
            public async Task CanExecuteAsync()
            {
                var key = Guid.NewGuid().ToString("N");

                await _db.JsonSetAsync(key, "{\"hello\": \"world\", \"goodnight\": {\"value\": \"moon\"}}");

                var result = await _db.JsonDeleteAsync(key, ".goodnight");
                var jsonResult = await _db.JsonGetAsync(key);

                Assert.Equal(1, result);
                Assert.DoesNotContain("goodnight", jsonResult.ToString());
            }
        }

        public class JsonMultiGetAsync : BaseIntegrationTest
        {
            [Fact]
            public async Task CanExecuteAsync()
            {
                var key1 = Guid.NewGuid().ToString("N");
                var key2 = Guid.NewGuid().ToString("N");

                await _db.JsonSetAsync(key1, "{\"hello\": \"world\", \"goodnight\": {\"value\": \"moon\"}}");
                await _db.JsonSetAsync(key2, "{\"hello\": \"tom\", \"goodnight\": {\"value\": \"tom\"}}");

                var result = await _db.JsonMultiGetAsync(new RedisKey[] { key1, key2 });

                Assert.Equal(2, result.Length);
                Assert.Contains("world", result[0].ToString());
                Assert.Contains("tom", result[1].ToString());
            }
        }

        public class JsonTypeAsync : BaseIntegrationTest
        {
            [Theory]
            [InlineData(".string", "string")]
            [InlineData(".integer", "integer")]
            [InlineData(".boolean", "boolean")]
            [InlineData(".number", "number")]
            public async Task CanExecuteAsync(string path, string value)
            {
                var key = Guid.NewGuid().ToString("N");

                await _db.JsonSetAsync(key, "{\"string\":\"hello world\", \"integer\":5, \"boolean\": true, \"number\":4.7}");

                var typeResult = await _db.JsonTypeAsync(key, path);

                Assert.Equal(value, typeResult.ToString());
            }
        }

        public class JsonIncrementNumberAsync : BaseIntegrationTest
        {
            [Theory]
            [InlineData(".integer", 1, 2)]
            [InlineData(".number", .9, 2)]
            public async Task CanExecuteAsync(string path, double number, double expectedResult)
            {
                var key = Guid.NewGuid().ToString("N");

                await _db.JsonSetAsync(key, "{\"integer\":1,\"number\":1.1}");

                var result = await _db.JsonIncrementNumberAsync(key, path, number);

                Assert.Equal(expectedResult, (double)result, 2);
            }
        }

        public class JsonMultiplyNumberAsync : BaseIntegrationTest
        {
            [Theory]
            [InlineData(".integer", 10, 10)]
            [InlineData(".number", .9, .99)]
            public async Task CanExecuteAsync(string path, double number, double expectedResult)
            {
                var key = Guid.NewGuid().ToString("N");

                await _db.JsonSetAsync(key, "{\"integer\":1,\"number\":1.1}");

                var result = await _db.JsonMultiplyNumberAsync(key, path, number);

                Assert.Equal(expectedResult, (double)result, 2);
            }
        }

        public class JsonAppendJsonStringAsync : BaseIntegrationTest
        {
            [Fact(Skip = "This doesn't work, not sure what I'm doing wrong yet.")]
            public async Task CanExecuteAsync()
            {
                var key = Guid.NewGuid().ToString("N");

                await _db.JsonSetAsync(key, "{\"hello\":\"world\"}");

                var result = await _db.JsonAppendJsonStringAsync(key, ".hello", "{\"t\":1}");

                Assert.Equal(4, result);
            }
        }

        public class JsonStringLengthAsync : BaseIntegrationTest
        {
            [Fact]
            public async Task CanExecuteAsync()
            {
                var key = Guid.NewGuid().ToString("N");

                await _db.JsonSetAsync(key, "{\"hello\":\"world\"}");

                var result = await _db.JsonStringLengthAsync(key, ".hello");

                Assert.Equal(5, result);
            }

            [Fact]
            public async Task WillReturnNullIfPathDoesntExist()
            {
                var key = Guid.NewGuid().ToString("N");

                await _db.JsonSetAsync(key, "{\"hello\":\"world\"}");

                var result = await _db.JsonStringLengthAsync("doesnt_exist", ".hello.doesnt.exist");

                Assert.Null(result);
            }
        }

        public class JsonArrayAppendAsync : BaseIntegrationTest
        {
            [Fact]
            public async Task CanExecuteAsync()
            {
                var key = Guid.NewGuid().ToString("N");

                await _db.JsonSetAsync(key, "{\"array\": []}");

                var result = await _db.JsonArrayAppendAsync(key, ".array", "\"hello\"", "\"world\"");

                Assert.Equal(2, result);
            }
        }

        public class JsonArrayIndexOfAsync : BaseIntegrationTest
        {
            [Fact]
            public async Task CanExecuteAsync()
            {
                var key = Guid.NewGuid().ToString();

                await _db.JsonSetAsync(key, "{\"array\": [\"hi\", \"world\", \"!\"]}");

                var result = await _db.JsonArrayIndexOfAsync(key, ".array", "\"world\"", 0, 2);

                Assert.Equal(1, result);
            }
        }

        public class JsonArrayInsertAsync : BaseIntegrationTest
        {
            [Fact]
            public async Task CanExecuteAsync()
            {
                var key = Guid.NewGuid().ToString();

                await _db.JsonSetAsync(key, "{\"array\": [\"hi\", \"world\", \"!\"]}");

                var result = await _db.JsonArrayInsertAsync(key, ".array", 1, "\"there\"");

                Assert.Equal(4, result);
            }
        }

        public class JsonArrayLengthAsync : BaseIntegrationTest
        {
            [Fact]
            public async Task CanExecute()
            {
                var key = Guid.NewGuid().ToString();

                await _db.JsonSetAsync(key, "{\"array\": [\"hi\", \"world\", \"!\"]}");

                var result = await _db.JsonArrayLengthAsync(key, ".array");

                Assert.Equal(3, result);
            }
        }

        public class JsonArrayPopAsync : BaseIntegrationTest
        {
            [Fact]
            public async Task CanExecuteAsync()
            {
                var key = Guid.NewGuid().ToString();

                await _db.JsonSetAsync(key, "{\"array\": [\"hi\", \"world\", \"!\"]}");

                var result = await _db.JsonArrayPopAsync(key, ".array", 1);

                Assert.Equal("\"world\"", result.ToString());
            }
        }

        public class JsonArrayTrimAsync : BaseIntegrationTest
        {
            [Fact]
            public async Task CanExecuteAsync()
            {
                var key = Guid.NewGuid().ToString();

                await _db.JsonSetAsync(key, "{\"array\": [\"hi\", \"world\", \"!\"]}");

                var result = await _db.JsonArrayTrimAsync(key, ".array", 0, 1);

                Assert.Equal(2, result);
            }
        }

        public class JsonObjectKeysAsync : BaseIntegrationTest
        {
            [Fact]
            public async Task CanExecuteAsync()
            {
                var key = Guid.NewGuid().ToString();

                await _db.JsonSetAsync(key, "{\"hello\": \"world\", \"goodnight\": {\"value\": \"moon\"}}");

                var result = await _db.JsonObjectKeysAsync(key);

                Assert.Equal(new[] { "hello", "goodnight" }, result.Select(x => x.ToString()).ToArray());
            }
        }

        public class JsonObjectLengthAsync : BaseIntegrationTest
        {
            [Fact]
            public async Task CanExecuteAsync()
            {
                var key = Guid.NewGuid().ToString();

                await _db.JsonSetAsync(key, "{\"hello\": \"world\", \"goodnight\": {\"value\": \"moon\"}}");

                var result = await _db.JsonObjectLengthAsync(key, ".goodnight");

                Assert.Equal(1, result);
            }
        }

        public class JsonDebugMemoryAsync : BaseIntegrationTest
        {
            [Fact]
            public async Task CanExecuteAsync()
            {
                var key = Guid.NewGuid().ToString();

                await _db.JsonSetAsync(key, "{\"hello\": \"world\", \"goodnight\": {\"value\": \"moon\"}}");

                var result = await _db.JsonDebugMemoryAsync(key, ".goodnight");

                Assert.Equal(89, result);
            }
        }

        public class JsonGetRespAsync : BaseIntegrationTest
        {
            [Fact]
            public async Task CanExecuteAsync()
            {
                var key = Guid.NewGuid().ToString();

                await _db.JsonSetAsync(key, "{\"hello\": \"world\", \"goodnight\": {\"value\": \"moon\"}}");

                var result = ((RedisResult[])(await _db.JsonGetRespAsync(key))[1])[1];

                Assert.Equal("world", result.ToString());
            }
        }
    }
}
