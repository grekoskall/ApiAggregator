using System;
using System.Text.Json;

namespace ApiAggregation.Models
{
    public class AggregatedData
    {
        public int Id { get; set; }
        public string Source { get; set; }
        public JsonElement Data { get; set; }
        public DateTime Timestamp { get; set; }
        public string DataType { get; set; }
    }
} 