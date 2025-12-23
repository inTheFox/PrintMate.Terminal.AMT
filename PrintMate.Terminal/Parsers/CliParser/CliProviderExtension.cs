using System;
using System.Collections.Generic;
using ProjectParserTest.Parsers.Shared.Models;
using System.Globalization;
using System.Linq;
using System.Text;
using ProjectParserTest.Parsers.Shared.Enums;

namespace ProjectParserTest.Parsers.CliParser
{
    public class DecodedInfo
    {
        public int PartId { get; set; }
        public int LaserNum { get; set; }
        public int RegionId { get; set; }
        public GeometryRegion Region { get; set; }

        public override string ToString()
        {
            return $"Decoded: PartID={PartId}, LaserNum={LaserNum}, RegionID={RegionId}, Region={Region}";
        }

        public void Print()
        {
            Console.WriteLine(ToString());
        }
    }
    public static class CliProviderExtension
    {

        public static DecodedInfo GetDecodedInfo(this CliProvider provider, int id, bool isOpen, bool isPoly,
            int fileVersion)
        {
            var dinfo = new DecodedInfo();
            dinfo.PartId = fileVersion > 6000 ? id / 10000 : id / 1000;
            dinfo.LaserNum = fileVersion > 6000 ? id % 10000 / 100 : id % 1000 / 10;
            dinfo.RegionId = fileVersion > 6000 ? id % 100 : id % 10;
            dinfo.Region = GetRegionByGeometryID(dinfo.RegionId, isPoly, isOpen, fileVersion);
            return dinfo;
        }

        public static int GetRegionId(int id, int fileVersion = 6000) => fileVersion > 6000 ? id % 100 : id % 10;
        public static int GetPartId(this Project project, int id)
        {
            int fileVersion = project.HeaderInfo.GetParameter(HeaderKeys.Info.VersionParameterKey).GetValue<int>();
            return fileVersion > 6000  ? id / 10000 : id / 1000;
        }

        public static int GetLaserNum(int id, int fileVersion = 6000) => fileVersion > 6000 ? id % 10000 / 100 : id % 1000 / 10;
        
        public static Part? GetPartById(this Project project, int id)
        {
            var partsParameter = project.HeaderInfo.GetParameter(HeaderKeys.Info.Parts);
            if (partsParameter == null) return null;
            return partsParameter.GetValue<List<Part>>()?.FirstOrDefault(p => p.Id == id/10000);
        }

        /// <summary>
        /// Получает регион геометрии по ID
        /// </summary>
        /// <param name="id">ID геометрии</param>
        /// <param name="isPoly">Это полилиния (true) или штриховка (false)</param>
        /// <param name="isOpen">Открытая (true) или закрытая (false) полилиния</param>
        /// <param name="fileVersion">Версия файла</param>
        public static GeometryRegion GetRegionByGeometryID(int id, bool isPoly, bool isOpen, int fileVersion)
        {
            if (fileVersion > 6000)
                return GetRegionByGeometryIDFV6(id % 100, isPoly, isOpen);
            return GetRegionByGeometryIDFVA6(id % 10, isPoly, isOpen);
        }

        private static GeometryRegion GetRegionByGeometryIDFVA6(int id, bool isPoly, bool isOpen)
        {
            switch (id)
            {
                case 0:
                    return isPoly ? GeometryRegion.Support : GeometryRegion.SupportFill;
                case 1:
                    return isPoly ? GeometryRegion.ContourDownskin : GeometryRegion.Downskin;
                case 2:
                    return isPoly ? GeometryRegion.Contour : GeometryRegion.Infill;
                case 3:
                    return isPoly ? GeometryRegion.ContourUpskin : GeometryRegion.Upskin;
                case 4:
                    return isPoly ? GeometryRegion.Edges : GeometryRegion.None;
                case 5:
                    if (isPoly)
                        return isOpen ? GeometryRegion.Infill : GeometryRegion.Contour;
                    return GeometryRegion.None;
                case 6:
                    if (isPoly)
                        return isOpen ? GeometryRegion.Upskin : GeometryRegion.ContourUpskin;
                    return GeometryRegion.None;
                case 7:
                    return isPoly ? GeometryRegion.Downskin : GeometryRegion.None;
                case 8:
                    return isPoly ? GeometryRegion.SupportFill : GeometryRegion.None;
                default:
                    return GeometryRegion.None;
            }
        }

        /// <summary>
        /// Получает регион геометрии по ID для файлов версии > 6000
        /// </summary>
        private static GeometryRegion GetRegionByGeometryIDFV6(int id, bool isPoly, bool isOpen)
        {
            switch (id)
            {
                case 0:
                    return isPoly ? GeometryRegion.Support : GeometryRegion.SupportFill;
                case 1:
                    return isPoly ? GeometryRegion.ContourDownskin : GeometryRegion.Downskin;
                case 2:
                    return isPoly ? GeometryRegion.Contour : GeometryRegion.Infill;
                case 3:
                    return isPoly ? GeometryRegion.ContourUpskin : GeometryRegion.Upskin;
                case 4:
                    return isPoly ? GeometryRegion.Edges : GeometryRegion.None;
                case 5:
                    if (isPoly)
                        return isOpen ? GeometryRegion.Infill : GeometryRegion.Contour;
                    return GeometryRegion.None;
                case 6:
                    if (isPoly)
                        return isOpen ? GeometryRegion.Upskin : GeometryRegion.ContourUpskin;
                    return GeometryRegion.None;
                case 7:
                    return isPoly ? GeometryRegion.Downskin : GeometryRegion.None;
                case 8:
                    return isPoly ? GeometryRegion.SupportFill : GeometryRegion.None;
                case 9:
                    return isPoly ? GeometryRegion.Support : GeometryRegion.None;
                case 10:
                    return isPoly ? GeometryRegion.Contour : GeometryRegion.None;
                case 11:
                    return isPoly ? GeometryRegion.ContourUpskin : GeometryRegion.None;
                case 12:
                    return isPoly ? GeometryRegion.DownskinRegionPreview : GeometryRegion.None;
                case 13:
                    return isPoly ? GeometryRegion.InfillRegionPreview : GeometryRegion.None;
                case 14:
                    return isPoly ? GeometryRegion.UpskinRegionPreview : GeometryRegion.None;
                default:
                    return GeometryRegion.None;
            }
        }

       
        public static void DumpHex(this CliProvider provider, byte[] bytes, int start, int length)
        {
            int end = Math.Min(start + length, bytes.Length);
            for (int i = start; i < end; i += 16)
            {
                string hex = "";
                string ascii = "";
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < end)
                    {
                        hex += $"{bytes[i + j]:X2} ";
                        ascii += bytes[i + j] >= 32 && bytes[i + j] <= 126 ? (char)bytes[i + j] : '.';
                    }
                    else
                    {
                        hex += "   ";
                        ascii += " ";
                    }
                }
                Console.WriteLine($"{i - start:X4}: {hex} | {ascii}");
            }
        }

        public static bool IsBinaryGeometry(this CliProvider provider, byte[] fileBytes, int startIndex)
        {
            if (startIndex + 2 > fileBytes.Length) return false;

            short tag = BitConverter.ToInt16(fileBytes, startIndex);
            return tag == 127 || tag == 128 || tag == 129 || tag == 130 || tag == 131 || tag == 132 || tag == 230 || tag == 232;
        }
        public static void ParseAsciiGeometry(this CliProvider provider, byte[] fileBytes, int startIndex, float unitsMultiplier)
        {
            string geometryText = Encoding.ASCII.GetString(fileBytes, startIndex, fileBytes.Length - startIndex);
            string[] lines = geometryText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            bool hasData = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("$$LAYER"))
                {
                    string heightStr = line.Substring(line.IndexOf('/') + 1);
                    if (float.TryParse(heightStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float height))
                    {
                        Console.WriteLine($"Layer: Height = {height * unitsMultiplier}");
                    }
                    else
                    {
                        Console.WriteLine($"Layer (raw): {line}");
                    }
                    hasData = true;
                }
                else if (line.StartsWith("$$POLYLINE") || line.StartsWith("$$HATCHES"))
                {
                    Console.WriteLine($"Geometry: {line}");
                    hasData = true;
                }
            }

            if (!hasData)
            {
                Console.WriteLine("No ASCII geometry lines found. Possibly empty or binary data misinterpreted.");
                provider.DumpHex(fileBytes, startIndex, Math.Min(256, fileBytes.Length - startIndex));
            }
        }
    }
}
