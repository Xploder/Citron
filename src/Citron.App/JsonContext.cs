using System.Text.Json.Serialization;
namespace Citron;
[JsonSerializable(typeof(Preferences))]
internal partial class JsonContext : JsonSerializerContext { }
