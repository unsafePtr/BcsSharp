using BcsSharp.Core;
using System;
using System.Collections.Generic;

var map = new Dictionary<string, int>
{
    ["zebra"] = 3,
    ["alpha"] = 1,
    ["beta"] = 2
};

var serialized = BcsSerializer.Serialize(map);
var reader = new BcsReader(serialized);

var count = reader.ReadULEB32();
Console.WriteLine($"Count: {count}");

for(int i = 0; i < count; i++)
{
    var key = reader.ReadString();
    var value = reader.ReadI32();
    Console.WriteLine($"Key {i+1}: '{key}' = {value}");
}
EOF < /dev/null
