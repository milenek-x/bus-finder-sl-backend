using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BusFinderBackend.Model // Ensure this matches your project's namespace
{
    public class GooglePlacesResponse
    {
        [JsonPropertyName("results")]
        public List<PlaceResult>? Results { get; set; }
        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    public class PlaceResult
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("geometry")]
        public Geometry? Geometry { get; set; }

        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; set; }

        [JsonPropertyName("place_id")]
        public string? PlaceId { get; set; }

        [JsonPropertyName("rating")]
        public double? Rating { get; set; }

        [JsonPropertyName("user_ratings_total")]
        public int? UserRatingsTotal { get; set; }
    }

    public class Geometry
    {
        [JsonPropertyName("location")]
        public Location? Location { get; set; }
    }

    public class Location
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }

}
