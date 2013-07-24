using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Device.Location;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace MetalWrench.VegasLocater
{
    public class VegasResolver : ICivicAddressResolver
    {
        struct Location
        {
            public String Name;
            public String PointOfInterestName;
            public GeoCoordinate Corner1;
            public GeoCoordinate Corner2;

            public override string ToString()
            {
                if (PointOfInterestName != null)
                    return PointOfInterestName + ", " + Name;
                else
                    return Name;
            }
        }

        // Looks like a good walking distance.
        // Note: Phone cell tower triangulation is about 350m accuracy.
        // Outside in Las Vegas, I was able to get about 47m accuracy from GPS.
        // Location also will use WiFi networks for triangulation - if a Phone knows network X is near location Y
        // after looking up info first, then it saves that somehow.  So theoretically that limits your accuracy to
        // the range of a wifi network, sometimes.
        private const double ErrorThresholdForSearching = 0.0015;
        private const double VeryPreciseErrorThreshold = 0.0004; // this is about 60 feet or so.

        private static readonly GeoCoordinate SeattleLocation = new GeoCoordinate(47.6130,-122.3378);
        private static readonly GeoCoordinate BellevueLocation = new GeoCoordinate(47.6095, -122.1738);
        private static readonly GeoCoordinate RedmondLocation = new GeoCoordinate(47.6632, -122.1079);
        private static readonly GeoCoordinate SnoqualmieLocation = new GeoCoordinate(47.5304, -121.8446);
        private static readonly GeoCoordinate VegasLocation = new GeoCoordinate(36.1200, -115.1727);
        private static readonly GeoCoordinate VegasFremontStreetLocation = new GeoCoordinate(36.1703, -115.1431);
        private static readonly GeoCoordinate RockfordIllinoisLocation = new GeoCoordinate(42.275, -89.05);
        private static readonly GeoCoordinate RoscoeIllinoisLocation = new GeoCoordinate(42.415, -89.0114);

        private List<Location> knownLocations;

        public VegasResolver() : this(false)
        {
        }

        public VegasResolver(bool includeAllCities)
        {
            List<Location> vegasLocations = ReadDataFile("VegasLocater.VegasCasinoLocations.txt");
            List<Location> seattleLocations = ReadDataFile("VegasLocater.SeattleLocations.txt");
            knownLocations = new List<Location>(vegasLocations.Count + seattleLocations.Count);
            knownLocations.AddRange(vegasLocations);
            knownLocations.AddRange(seattleLocations);

            if (includeAllCities)
            {
                List<Location> rockfordLocations = ReadDataFile("VegasLocater.RockfordLocations.txt");
                knownLocations.AddRange(rockfordLocations);
            }
        }

        private String ResolveCity(GeoCoordinate coordinate, out String state)
        {
            state = null;

            const double SeattleSizeThreshold = 0.0500;
            if (IsDistanceWithin(coordinate, SeattleLocation, SeattleSizeThreshold))
            {
                state = "Washington";
                return "Seattle";
            }

            const double BellevueSizeThreshold = 0.0500;
            if (IsDistanceWithin(coordinate, BellevueLocation, BellevueSizeThreshold))
            {
                state = "Washington";
                return "Bellevue";
            }

            const double RedmondSizeThreshold = 0.0360;  // 0.0360 should get most of the Microsoft main campus.  But it's a tricky area.
            if (IsDistanceWithin(coordinate, RedmondLocation, RedmondSizeThreshold))
            {
                state = "Washington";
                return "Redmond";
            }

            const double SnoqualmieSizeThreshold = 0.0250;
            if (IsDistanceWithin(coordinate, SnoqualmieLocation, SnoqualmieSizeThreshold))
            {
                state = "Washington";
                return "Snoqualmie";
            }

            const double VegasSizeThreshold = 0.0700;
            if (IsDistanceWithin(coordinate, VegasLocation, VegasSizeThreshold))
            {
                state = "Nevada";
                return "Las Vegas";
            }

            const double VegasDowntownSizeThreshold = 0.0050;
            if (IsDistanceWithin(coordinate, VegasFremontStreetLocation, VegasDowntownSizeThreshold))
            {
                state = "Nevada";
                return "Las Vegas - Fremont Street";
            }

            const double RockfordIllinoisSizeThreshold = 0.1030;
            if (IsDistanceWithin(coordinate, RockfordIllinoisLocation, RockfordIllinoisSizeThreshold))
            {
                state = "Illinois";
                return "Rockford";
            }

            const double RoscoeIllinoisSizeThreshold = 0.0200;
            if (IsDistanceWithin(coordinate, RoscoeIllinoisLocation, RoscoeIllinoisSizeThreshold))
            {
                state = "Illinois";
                return "Roscoe";
            }

            return null;
        }

        private static bool IsDistanceWithin(GeoCoordinate coordinate, GeoCoordinate knownLocation, double distanceThresholdInDegrees)
        {
            double diffLat = Math.Abs(coordinate.Latitude - knownLocation.Latitude);
            double diffLong = Math.Abs(coordinate.Longitude - knownLocation.Longitude);
            double distance = Math.Sqrt(diffLat * diffLat + diffLong * diffLong);
            return distance < distanceThresholdInDegrees;
        }

        private static List<Location> ReadDataFile(String locationsFileName)
        {
            List<Location> locations = new List<Location>();
            Stream s = typeof(VegasResolver).Assembly.GetManifestResourceStream(locationsFileName);
            using (StreamReader reader = new StreamReader(s))
            {
                String line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length == 0 || String.IsNullOrWhiteSpace(line) || line[0]=='#')
                        continue;

                    String[] parts = line.Split('|');
                    String placeName = parts[0];
                    String poiName = null;
                    int atSignIndex = placeName.IndexOf('@');
                    if (atSignIndex > 0) {
                        poiName = placeName.Substring(0, atSignIndex);
                        placeName = placeName.Substring(atSignIndex + 1);
                    }

                    // I wasn't uniform about these...
                    GeoCoordinate upperLeft = ParseGeoCoordinate(parts[1]);
                    GeoCoordinate lowerRight = ParseGeoCoordinate(parts[2]);

                    Location loc = new Location();
                    loc.Name = placeName;
                    loc.PointOfInterestName = poiName;
                    loc.Corner1 = upperLeft;
                    loc.Corner2 = lowerRight;
                    locations.Add(loc);
                }
            }

            return locations;
        }

        // Parses a string like "36.1234,-115.1993"
        private static GeoCoordinate ParseGeoCoordinate(String geoCoordinate)
        {
            String[] latLong = geoCoordinate.Split(',');
            Contract.Assert(latLong.Length == 2);
            double latitude = Double.Parse(latLong[0], CultureInfo.InvariantCulture);
            double longitude = Double.Parse(latLong[1], CultureInfo.InvariantCulture);
            Contract.Assert(latitude >= -90 && latitude <= 90, "Latitude should be between -90 and 90");
            Contract.Assert(longitude >= -180 && longitude <= 180, "Longitude should be between -180 and 180");
            GeoCoordinate coord = new GeoCoordinate(latitude, longitude);
            return coord;
        }

        public bool AllowWideRangeForErrors
        {
            get;
            set;
        }

        public CivicAddress ResolveAddress(GeoCoordinate coordinate)
        {
            Contract.Requires(coordinate != null);

            String addressLine1 = null, addressLine2 = null, building = null, city = null;
            String countryRegion = null;
            String state = null;

            city = ResolveCity(coordinate, out state);
            if (city == null)
                city = "<unknown>";

            double errorThreshold = AllowWideRangeForErrors ? ErrorThresholdForSearching : VeryPreciseErrorThreshold;

            // @TODO: My data files are a little inconsistent.  For Vegas, the building is usually the second option, like loc.Name.
            // For Microsoft main campus, the building is the point of interest, while the main campus is the name.
            IList<Location> locs = FindLocations(coordinate, errorThreshold);
            if (locs.Count > 0)
            {
                building = locs[0].Name;
                if (locs[0].PointOfInterestName != null)
                {
                    addressLine1 = locs[0].PointOfInterestName;
                    addressLine2 = locs[0].Name;
                    building = locs[0].Name;
                }
                else 
                    addressLine1 = building;

                if (locs.Count > 1)
                {
                    foreach (Location loc in locs)
                    {
                        if (loc.PointOfInterestName != null)
                        {
                            addressLine1 = loc.PointOfInterestName;
                            addressLine2 = loc.Name;
                            if (building == null)
                                building = loc.Name;
                            break;
                        }
                    }
                }
            }
            CivicAddress address = new CivicAddress(addressLine1, addressLine2, building, city, countryRegion, null, null, state);

            EventHandler<ResolveAddressCompletedEventArgs> handler = ResolveAddressCompleted;
            if (handler != null)
            {
                ResolveAddressCompletedEventArgs args = new ResolveAddressCompletedEventArgs(address, null, false, null);
                handler(this, args);
            }

            return address;
        }

        private IList<Location> FindLocations(GeoCoordinate coordinate)
        {
            List<Location> matchingLocations = new List<Location>();

            foreach (Location loc in knownLocations)
            {
                //Debug.Assert(loc.Name != "Venetian");
                if (IsWithinBoundingBox(coordinate, loc.Corner1, loc.Corner2))
                    matchingLocations.Add(loc);
            }
            return matchingLocations;
        }

        // 0.0015 seems like a good threshold for finding nearby buildings.
        private IList<Location> FindLocations(GeoCoordinate coordinate, double errorThreshold)
        {
            List<Location> matchingLocations = new List<Location>();

            // First try an exact match.  If that fails, then try with an error.
            foreach (Location loc in knownLocations)
            {
                //Debug.Assert(loc.Name != "Venetian");
                if (IsWithinBoundingBox(coordinate, loc.Corner1, loc.Corner2))
                    matchingLocations.Add(loc);
            }

            if (matchingLocations.Count == 0)
            {
                foreach (Location loc in knownLocations)
                {
                    if (IsWithinBoundingBox(coordinate, loc.Corner1, loc.Corner2, errorThreshold))
                        matchingLocations.Add(loc);
                }
            }

            return matchingLocations;
        }

        private static bool IsWithinBoundingBox(GeoCoordinate coordinate, GeoCoordinate corner1, GeoCoordinate corner2)
        {
            double lat1 = corner1.Latitude, lat2 = corner2.Latitude;
            if (lat1 < lat2)
            {
                double tmp = lat1;
                lat1 = lat2;
                lat2 = tmp;
            }

            double long1 = corner1.Longitude, long2 = corner2.Longitude;
            if (long1 < long2)
            {
                double tmp = long1;
                long1 = long2;
                long2 = tmp;
            }

            Contract.Assert(lat1 >= lat2, "Latitudes don't line up");
            Contract.Assert(long1 >= long2, "Longitudes don't line up");
            return (lat1 >= coordinate.Latitude && coordinate.Latitude >= lat2) &&
                (long1 >= coordinate.Longitude && coordinate.Longitude >= long2);
        }

        private static bool IsWithinBoundingBox(GeoCoordinate coordinate, GeoCoordinate corner1, GeoCoordinate corner2, double errorThreshold)
        {
            double lat1 = corner1.Latitude, lat2 = corner2.Latitude;
            if (lat1 < lat2)
            {
                double tmp = lat1;
                lat1 = lat2;
                lat2 = tmp;
            }

            double long1 = corner1.Longitude, long2 = corner2.Longitude;
            if (long1 < long2)
            {
                double tmp = long1;
                long1 = long2;
                long2 = tmp;
            }

            Contract.Assert(lat1 >= lat2, "Latitudes don't line up");
            Contract.Assert(long1 >= long2, "Longitudes don't line up");
            return (lat1 + errorThreshold >= coordinate.Latitude && coordinate.Latitude >= lat2 - errorThreshold) &&
                (long1 + errorThreshold >= coordinate.Longitude && coordinate.Longitude >= long2 - errorThreshold);
        }

        public void ResolveAddressAsync(GeoCoordinate coordinate)
        {
            ThreadPool.QueueUserWorkItem((Object state) =>
            {
                ResolveAddress(coordinate);
            });
        }

        public event EventHandler<ResolveAddressCompletedEventArgs> ResolveAddressCompleted;
    }

    /* Distance-finding code...  Does System.Device.Location have a way to subtract coordinates and get meters?
     
        'below is from
'http://www.zipcodeworld.com/samples/distance.vbnet.html
Public Function distance(ByVal lat1 As Double, ByVal lon1 As Double, _
                         ByVal lat2 As Double, ByVal lon2 As Double, _
                         Optional ByVal unit As Char = "M"c) As Double
    Dim theta As Double = lon1 - lon2
    Dim dist As Double = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) + _
                            Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) * _
                            Math.Cos(deg2rad(theta))
    dist = Math.Acos(dist)
    dist = rad2deg(dist)
    dist = dist * 60 * 1.1515
    If unit = "K" Then
        dist = dist * 1.609344              <------  Not sure where these constants come from.  May need different ones for each region of earth!
    ElseIf unit = "N" Then                           Read up on UTM (spherical projection of earth's surface) http://www.dmap.co.uk/utmworld.htm
        dist = dist * 0.8684
    End If
    Return dist
End Function
Public Function Haversine(ByVal lat1 As Double, ByVal lon1 As Double, _
                         ByVal lat2 As Double, ByVal lon2 As Double, _
                         Optional ByVal unit As Char = "M"c) As Double
    Dim R As Double = 6371 'earth radius in km
    Dim dLat As Double
    Dim dLon As Double
    Dim a As Double
    Dim c As Double
    Dim d As Double
    dLat = deg2rad(lat2 - lat1)
    dLon = deg2rad((lon2 - lon1))
    a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(deg2rad(lat1)) * _
            Math.Cos(deg2rad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2)
    c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a))
    d = R * c
    Select Case unit.ToString.ToUpper
        Case "M"c
            d = d * 0.62137119
        Case "N"c
            d = d * 0.5399568
    End Select
    Return d
End Function
Private Function deg2rad(ByVal deg As Double) As Double
    Return (deg * Math.PI / 180.0)
End Function
Private Function rad2deg(ByVal rad As Double) As Double
    Return rad / Math.PI * 180.0
End Function

     */
}
