using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Tools.CSIxRevit.Models;

namespace JNNTool.Tools.CSIxRevit.Parser
{
    public class E2KParser
    {
        private double scaleToFeet = 3.2808399; // Default Meters to Feet

        public void Parse(Dictionary<string, List<string>> tables,
            out List<PointData> points,
            out List<BeamData> beams,
            out List<ColumnData> columns,
            out List<WallData> walls,
            out List<FloorData> floors,
            out Dictionary<string, string> sectionProperties,
            out List<StoryData> stories,
            out List<GridData> grids)
        {
            points = new List<PointData>();
            beams = new List<BeamData>();
            columns = new List<ColumnData>();
            walls = new List<WallData>();
            floors = new List<FloorData>();
            sectionProperties = new Dictionary<string, string>();
            stories = new List<StoryData>();
            grids = new List<GridData>();

            // 1. Determine Units
            var controlKeys = tables.Keys.Where(k => k.Contains("CONTROL")).ToList();
            foreach (var key in controlKeys)
            {
                foreach (var line in tables[key])
                {
                    if (line.Contains("\"MM\"")) scaleToFeet = 1.0 / 304.8;
                    else if (line.Contains("\"IN\"")) scaleToFeet = 1.0 / 12.0;
                    else if (line.Contains("\"M\"")) scaleToFeet = 1.0 / 0.3048;
                    else if (line.Contains("\"CM\"")) scaleToFeet = 1.0 / 30.48;
                }
            }

            var localPoints = new Dictionary<string, PointData>(StringComparer.OrdinalIgnoreCase);
            var localBeams = new Dictionary<string, BeamData>(StringComparer.OrdinalIgnoreCase);
            var localColumns = new Dictionary<string, ColumnData>(StringComparer.OrdinalIgnoreCase);
            var localWalls = new Dictionary<string, WallData>(StringComparer.OrdinalIgnoreCase);
            var localFloors = new Dictionary<string, FloorData>(StringComparer.OrdinalIgnoreCase);

            // Generic Frame and Area dictionaries for connectivity (geometry only)
            var frameConnectivity = new Dictionary<string, FrameConnData>(StringComparer.OrdinalIgnoreCase);
            var areaConnectivity = new Dictionary<string, AreaConnData>(StringComparer.OrdinalIgnoreCase);

            // Per-story assign lists
            var frameAssigns = new List<FrameAssignData>();
            var areaAssigns = new List<AreaAssignData>();

            sectionProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var sectionPropTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 1.5 Parse Section Properties
            var secKeys = tables.Keys.Where(k => k.Contains("SECTION PROP") || k.Contains("FRAME SECTIONS") || k.Contains("AREA SECTIONS") || k.Contains("SLAB PROP") || k.Contains("WALL PROP") || k.Contains("DECK PROP")).ToList();
            foreach (var key in secKeys)
            {
                foreach (var line in tables[key])
                {
                    var tokens = Tokenize(line);
                    if (tokens.Count < 2) continue;

                    string name = Clean(tokens[1]);

                    // Parse PROPTYPE (Wall, Slab, Deck) from SHELLPROP definitions
                    int propTypeIdx = tokens.FindIndex(t => t.Equals("PROPTYPE", StringComparison.OrdinalIgnoreCase));
                    if (propTypeIdx >= 0 && propTypeIdx + 1 < tokens.Count)
                    {
                        sectionPropTypes[name] = Clean(tokens[propTypeIdx + 1]).ToUpper();
                    }

                    var strTokens = tokens.Where(t => !TryParseDouble(t, out _)).Select(t => Clean(t)).ToList();
                    var numTokens = tokens.Where(t => TryParseDouble(t, out _)).Select(t => Clean(t)).ToList();

                    string details = "";
                    if (numTokens.Count > 0)
                    {
                        details = "Size: " + string.Join("x", numTokens.Take(3));
                    }
                    var extraStrs = strTokens.Skip(2).Where(t => !t.Equals("Rectangular", StringComparison.OrdinalIgnoreCase) && !t.Equals("Circular", StringComparison.OrdinalIgnoreCase) && !t.Equals("Concrete", StringComparison.OrdinalIgnoreCase) && !t.Equals("Steel", StringComparison.OrdinalIgnoreCase)).ToList();
                    if (extraStrs.Count > 0)
                    {
                        details += " - Mat: " + string.Join(",", extraStrs);
                    }
                    if (!sectionProperties.ContainsKey(name))
                    {
                        sectionProperties[name] = details.Trim(' ', '-');
                    }
                }
            }

            // 1.5 Parse Stories
            var storyKeys = tables.Keys.Where(k => k.Contains("STOR")).ToList();
            foreach (var key in storyKeys)
            {
                double currentElev = 0;
                var reversedLines = tables[key].ToList();
                reversedLines.Reverse();
                foreach (var line in reversedLines)
                {
                    var tokens = Tokenize(line);
                    if (tokens.Count >= 2 && (tokens[0].Equals("STORY", StringComparison.OrdinalIgnoreCase) || tokens.Contains("STORY")))
                    {
                        string name = Clean(tokens[1]);
                        if (name.Equals("Base", StringComparison.OrdinalIgnoreCase))
                        {
                            int elevIdx = tokens.FindIndex(t => t.Equals("ELEV", StringComparison.OrdinalIgnoreCase));
                            if (elevIdx >= 0 && elevIdx + 1 < tokens.Count) currentElev = ParseDouble(tokens[elevIdx + 1]);
                            stories.Insert(0, new StoryData { Name = name, Elevation = currentElev * scaleToFeet, Height = 0 });
                        }
                        else
                        {
                            int hIdx = tokens.FindIndex(t => t.Equals("HEIGHT", StringComparison.OrdinalIgnoreCase));
                            double height = 0;
                            if (hIdx >= 0 && hIdx + 1 < tokens.Count) height = ParseDouble(tokens[hIdx + 1]);
                            currentElev += height;
                            stories.Insert(0, new StoryData { Name = name, Elevation = currentElev * scaleToFeet, Height = height * scaleToFeet });
                        }
                    }
                }
            }

            // 1.6 Parse Grids
            var gridKeys = tables.Keys.Where(k => k.Contains("GRID")).ToList();
            foreach (var key in gridKeys)
            {
                foreach (var line in tables[key])
                {
                    var tokens = Tokenize(line);
                    if (tokens.Count >= 2 && tokens[0].Equals("GRID", StringComparison.OrdinalIgnoreCase))
                    {
                        int lblIdx = tokens.FindIndex(t => t.Equals("LABEL", StringComparison.OrdinalIgnoreCase));
                        int dirIdx = tokens.FindIndex(t => t.Equals("DIR", StringComparison.OrdinalIgnoreCase));
                        int coordIdx = tokens.FindIndex(t => t.Equals("COORD", StringComparison.OrdinalIgnoreCase));
                        
                        if (lblIdx >= 0 && dirIdx >= 0 && coordIdx >= 0 && coordIdx + 1 < tokens.Count)
                        {
                            grids.Add(new GridData {
                                Name = Clean(tokens[lblIdx + 1]),
                                Direction = Clean(tokens[dirIdx + 1]),
                                Coordinate = ParseDouble(tokens[coordIdx + 1]) * scaleToFeet
                            });
                        }
                    }
                }
            }

            // 2. Parse Points
            var pointKeys = tables.Keys.Where(k => k.Contains("POINT COORDINATES") || k.Contains("JOINT COORDINATES") || k.Contains("POINT COOR")).ToList();
            foreach (var key in pointKeys)
            {
                foreach (var line in tables[key])
                {
                    var tokens = Tokenize(line);
                    if (tokens.Count >= 2)
                    {
                        string name = Clean(tokens[0]);
                        if (name.Equals("POINT", StringComparison.OrdinalIgnoreCase) || name.Equals("JOINT", StringComparison.OrdinalIgnoreCase))
                            name = Clean(tokens[1]);

                        double x = 0, y = 0, z = 0;
                        bool isValid = false;

                        int xIdx = tokens.FindIndex(t => t.Equals("X", StringComparison.OrdinalIgnoreCase));
                        int yIdx = tokens.FindIndex(t => t.Equals("Y", StringComparison.OrdinalIgnoreCase));
                        int zIdx = tokens.FindIndex(t => t.Equals("Z", StringComparison.OrdinalIgnoreCase));

                        if (xIdx >= 0 && xIdx + 1 < tokens.Count) { x = ParseDouble(tokens[xIdx + 1]); isValid = true; }
                        if (yIdx >= 0 && yIdx + 1 < tokens.Count) { y = ParseDouble(tokens[yIdx + 1]); isValid = true; }
                        if (zIdx >= 0 && zIdx + 1 < tokens.Count) { z = ParseDouble(tokens[zIdx + 1]); isValid = true; }

                        if (!isValid)
                        {
                            int startIndex = (tokens[0].Equals("POINT", StringComparison.OrdinalIgnoreCase) || tokens[0].Equals("JOINT", StringComparison.OrdinalIgnoreCase)) ? 2 : 1;
                            var numTokens = new List<double>();
                            for (int i = startIndex; i < tokens.Count; i++)
                            {
                                if (TryParseDouble(tokens[i], out double v))
                                    numTokens.Add(v);
                            }
                            
                            if (numTokens.Count >= 3)
                            {
                                x = numTokens[0]; y = numTokens[1]; z = numTokens[2]; isValid = true;
                            }
                            else if (numTokens.Count == 2)
                            {
                                x = numTokens[0]; y = numTokens[1]; z = 0; isValid = true;
                            }
                        }

                        if (isValid && !string.IsNullOrEmpty(name))
                        {
                            localPoints[name] = new PointData { Name = name, Position = new XYZ(x * scaleToFeet, y * scaleToFeet, z * scaleToFeet) };
                        }
                    }
                }
            }

            // 3. Phase 1: Parse Frame Connectivity (geometry only — no story/section)
            var connKeys = tables.Keys.Where(k => k.Contains("CONNECTIVIT") || k.Contains("LINE CONN")).ToList();
            // Also include generic LINE/FRAME/BEAM/COLUMN keys that are NOT assign keys
            var otherFrameKeys = tables.Keys.Where(k => (k.Contains("LINE") || k.Contains("FRAME") || k.Contains("BEAM") || k.Contains("COLUMN"))
                && !k.Contains("AREA") && !k.Contains("SHELL") && !k.Contains("ASSIGN") && !k.Contains("LOAD") && !k.Contains("OBJECT")).ToList();
            var allConnKeys = connKeys.Union(otherFrameKeys, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var key in allConnKeys)
            {
                foreach (var line in tables[key])
                {
                    var tokens = Tokenize(line);
                    if (tokens.Count < 2) continue;
                    
                    string firstToken = tokens[0].ToUpper();
                    // Skip LINEASSIGN lines that may be mixed into connectivity tables
                    if (firstToken.Contains("ASSIGN")) continue;

                    bool isTokLabel = firstToken == "LINE" || firstToken == "FRAME" || firstToken == "BEAM" || firstToken == "COLUMN";
                    
                    string name = Clean(tokens[0]);
                    if (isTokLabel && tokens.Count > 1) name = Clean(tokens[1]);

                    if (!frameConnectivity.ContainsKey(name)) frameConnectivity[name] = new FrameConnData { Name = name };

                    // Try to find Points in this line
                    int idxI = tokens.FindIndex(t => t.Equals("POINTI", StringComparison.OrdinalIgnoreCase) || t.Equals("JOINTI", StringComparison.OrdinalIgnoreCase) || t.Equals("I", StringComparison.OrdinalIgnoreCase));
                    int idxJ = tokens.FindIndex(t => t.Equals("POINTJ", StringComparison.OrdinalIgnoreCase) || t.Equals("JOINTJ", StringComparison.OrdinalIgnoreCase) || t.Equals("J", StringComparison.OrdinalIgnoreCase));
                    
                    if (idxI >= 0 && idxI + 1 < tokens.Count) frameConnectivity[name].PointI = Clean(tokens[idxI + 1]);
                    if (idxJ >= 0 && idxJ + 1 < tokens.Count) frameConnectivity[name].PointJ = Clean(tokens[idxJ + 1]);

                    if (string.IsNullOrEmpty(frameConnectivity[name].PointI))
                    {
                        int startIdx = isTokLabel ? 2 : 1;
                        if (startIdx < tokens.Count)
                        {
                            string typeToken = Clean(tokens[startIdx]).ToUpper();
                            if (typeToken == "BEAM" || typeToken == "COLUMN" || typeToken == "BRACE" || typeToken == "NONE" || typeToken == "LINE")
                            {
                                startIdx++;
                            }
                            
                            if (startIdx + 1 < tokens.Count)
                            {
                                frameConnectivity[name].PointI = Clean(tokens[startIdx]);
                                frameConnectivity[name].PointJ = Clean(tokens[startIdx + 1]);
                            }
                        }
                    }
                    
                    if (firstToken == "COLUMN" || key.Contains("COLUMN")) frameConnectivity[name].IsColumn = true;
                    if (firstToken == "BEAM" || key.Contains("BEAM")) frameConnectivity[name].IsColumn = false;
                    // Detect from connectivity type token (LINE "C3" COLUMN "15" "15")
                    if (tokens.Count > 2)
                    {
                        string t2 = Clean(tokens[isTokLabel ? 2 : 1]).ToUpper();
                        if (t2 == "COLUMN") frameConnectivity[name].IsColumn = true;
                        else if (t2 == "BEAM") frameConnectivity[name].IsColumn = false;
                    }
                }
            }

            // 3b. Phase 2: Parse LINE ASSIGNS (create per-story instances)
            var lineAssignKeys = tables.Keys.Where(k => k.Contains("LINE ASSIGN")).ToList();
            // Also check inside generic LINE/FRAME keys for LINEASSIGN lines
            var mixedLineKeys = tables.Keys.Where(k => (k.Contains("LINE") || k.Contains("FRAME"))
                && !k.Contains("AREA") && !k.Contains("SHELL") && !k.Contains("LOAD") && !k.Contains("OBJECT")).ToList();
            var allLineAssignKeys = lineAssignKeys.Union(mixedLineKeys, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var key in allLineAssignKeys)
            {
                foreach (var line in tables[key])
                {
                    var tokens = Tokenize(line);
                    if (tokens.Count < 3) continue;
                    
                    string firstToken = tokens[0].ToUpper();
                    if (firstToken != "LINEASSIGN") continue;

                    string name = Clean(tokens[1]);
                    string storyName = Clean(tokens[2]);
                    string section = "DefaultFrame";

                    int sIdx = tokens.FindIndex(t => t.Equals("SECTION", StringComparison.OrdinalIgnoreCase) || t.Equals("PROP", StringComparison.OrdinalIgnoreCase));
                    if (sIdx >= 0 && sIdx + 1 < tokens.Count)
                    {
                        section = Clean(tokens[sIdx + 1]);
                    }

                    frameAssigns.Add(new FrameAssignData { FrameName = name, StoryName = storyName, Section = section });
                }
            }

            // 4. Phase 1: Parse Area Connectivity (geometry only — no story/section)
            var areaConnKeys = tables.Keys.Where(k => (k.Contains("AREA") || k.Contains("SHELL") || k.Contains("WALL") || k.Contains("FLOOR") || k.Contains("SLAB") || k.Contains("DECK"))
                && !k.Contains("ASSIGN") && !k.Contains("PROP") && !k.Contains("LOAD") && !k.Contains("OBJECT") && !k.Contains("DESIGN")).ToList();
            foreach (var key in areaConnKeys)
            {
                foreach (var line in tables[key])
                {
                    var tokens = Tokenize(line);
                    if (tokens.Count < 2) continue;
                    
                    string firstToken = tokens[0].ToUpper();
                    // Skip AREAASSIGN lines
                    if (firstToken.Contains("ASSIGN")) continue;

                    bool isTokLabel = firstToken == "AREA" || firstToken == "SHELL" || firstToken == "WALL" || firstToken == "FLOOR" || firstToken == "SLAB";
                    
                    string name = Clean(tokens[0]);
                    if (isTokLabel && tokens.Count > 1) name = Clean(tokens[1]);

                    if (!areaConnectivity.ContainsKey(name)) areaConnectivity[name] = new AreaConnData { Name = name };

                    if (areaConnectivity[name].PointNames.Count == 0)
                    {
                        int startIdx = isTokLabel ? 2 : 1;
                        if (startIdx < tokens.Count)
                        {
                            string typeToken = Clean(tokens[startIdx]).ToUpper();
                            if (typeToken == "FLOOR" || typeToken == "WALL" || typeToken == "SLAB" || typeToken == "DECK" || typeToken == "PANEL" || typeToken == "NONE")
                            {
                                areaConnectivity[name].ElementType = typeToken;
                                startIdx++;
                            }
                            
                            if (startIdx < tokens.Count && int.TryParse(Clean(tokens[startIdx]), out int ptCount))
                            {
                                startIdx++;
                                for (int i = 0; i < ptCount && startIdx + i < tokens.Count; i++)
                                {
                                    string ptName = Clean(tokens[startIdx + i]);
                                    if (!areaConnectivity[name].PointNames.Contains(ptName))
                                    {
                                        areaConnectivity[name].PointNames.Add(ptName);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 4b. Phase 2: Parse AREA ASSIGNS (create per-story instances)
            var areaAssignKeys = tables.Keys.Where(k => k.Contains("AREA ASSIGN")).ToList();
            // Also check inside generic AREA keys for AREAASSIGN lines
            var mixedAreaKeys = tables.Keys.Where(k => (k.Contains("AREA") || k.Contains("SHELL"))
                && !k.Contains("LOAD") && !k.Contains("OBJECT") && !k.Contains("PROP") && !k.Contains("DESIGN")).ToList();
            var allAreaAssignKeys = areaAssignKeys.Union(mixedAreaKeys, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var key in allAreaAssignKeys)
            {
                foreach (var line in tables[key])
                {
                    var tokens = Tokenize(line);
                    if (tokens.Count < 3) continue;
                    
                    string firstToken = tokens[0].ToUpper();
                    if (firstToken != "AREAASSIGN") continue;

                    string name = Clean(tokens[1]);
                    string storyName = Clean(tokens[2]);
                    string section = "DefaultArea";

                    int sIdx = tokens.FindIndex(t => t.Equals("SECTION", StringComparison.OrdinalIgnoreCase) || t.Equals("PROP", StringComparison.OrdinalIgnoreCase));
                    if (sIdx >= 0 && sIdx + 1 < tokens.Count)
                    {
                        section = Clean(tokens[sIdx + 1]);
                    }

                    areaAssigns.Add(new AreaAssignData { AreaName = name, StoryName = storyName, Section = section });
                }
            }

            // Distribute Frames to Beams/Columns using per-story assigns
            // Track which frames got assigned so we can fallback for unassigned ones
            var assignedFrames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var assign in frameAssigns)
            {
                if (!frameConnectivity.ContainsKey(assign.FrameName)) continue;
                var conn = frameConnectivity[assign.FrameName];

                if (string.IsNullOrEmpty(conn.PointI) || string.IsNullOrEmpty(conn.PointJ)) continue;
                if (!localPoints.ContainsKey(conn.PointI) || !localPoints.ContainsKey(conn.PointJ)) continue;

                assignedFrames.Add(assign.FrameName);

                var ptI = localPoints[conn.PointI].Position;
                var ptJ = localPoints[conn.PointJ].Position;

                // Detect Z offsets from POINT COORDINATES (e.g. water tank columns/beams)
                // Points like "15-1" have non-zero Z = local offset within the story
                double zOffsetI = ptI.Z;
                double zOffsetJ = ptJ.Z;
                bool hasZOffset = Math.Abs(zOffsetI) > 0.01 || Math.Abs(zOffsetJ) > 0.01;

                var story = stories.FirstOrDefault(s => s.Name.Equals(assign.StoryName, StringComparison.OrdinalIgnoreCase));
                double elev = story != null ? story.Elevation : 0;
                double botElev = 0;
                if (story != null) {
                    int idx = stories.IndexOf(story);
                    if (idx < stories.Count - 1) botElev = stories[idx + 1].Elevation;
                    else botElev = story.Elevation;
                }

                string section = assign.Section != "DefaultFrame" ? assign.Section : "DefaultFrame";
                string instanceName = assign.FrameName + "_" + assign.StoryName;

                bool isVertical = Math.Abs(ptI.X - ptJ.X) < 0.1 && Math.Abs(ptI.Y - ptJ.Y) < 0.1;
                if (conn.IsColumn == true || (conn.IsColumn == null && isVertical))
                {
                    double colTop = elev;
                    double colBot = botElev;
                    if (hasZOffset)
                    {
                        // Z-offset column (e.g. water tank): Z is relative to bottom of story
                        colBot = botElev + Math.Min(zOffsetI, zOffsetJ);
                        colTop = botElev + Math.Max(zOffsetI, zOffsetJ);
                    }
                    columns.Add(new ColumnData { Name = instanceName, PointI = conn.PointI, PointJ = conn.PointJ, Section = section, TopElevation = colTop, BottomElevation = colBot });
                }
                else
                {
                    double beamElev = elev;
                    if (hasZOffset)
                    {
                        // Z-offset beam: Z is relative to bottom of story
                        beamElev = botElev + Math.Max(zOffsetI, zOffsetJ);
                    }
                    beams.Add(new BeamData { Name = instanceName, PointI = conn.PointI, PointJ = conn.PointJ, Section = section, Elevation = beamElev });
                }
            }

            // Fallback: frames in connectivity but without any LINEASSIGN → create at elevation 0
            foreach (var conn in frameConnectivity.Values)
            {
                if (assignedFrames.Contains(conn.Name)) continue;
                if (string.IsNullOrEmpty(conn.PointI) || string.IsNullOrEmpty(conn.PointJ)) continue;
                if (!localPoints.ContainsKey(conn.PointI) || !localPoints.ContainsKey(conn.PointJ)) continue;

                var ptI = localPoints[conn.PointI].Position;
                var ptJ = localPoints[conn.PointJ].Position;

                bool isVertical = Math.Abs(ptI.X - ptJ.X) < 0.1 && Math.Abs(ptI.Y - ptJ.Y) < 0.1;
                if (conn.IsColumn == true || (conn.IsColumn == null && isVertical))
                {
                    columns.Add(new ColumnData { Name = conn.Name, PointI = conn.PointI, PointJ = conn.PointJ, Section = "DefaultFrame", TopElevation = 0, BottomElevation = 0 });
                }
                else
                {
                    beams.Add(new BeamData { Name = conn.Name, PointI = conn.PointI, PointJ = conn.PointJ, Section = "DefaultFrame", Elevation = 0 });
                }
            }

            // Distribute Areas to Walls/Floors using per-story assigns
            var assignedAreas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var assign in areaAssigns)
            {
                if (!areaConnectivity.ContainsKey(assign.AreaName)) continue;
                var conn = areaConnectivity[assign.AreaName];

                if (conn.PointNames.Count < 2) continue;

                assignedAreas.Add(assign.AreaName);

                var story = stories.FirstOrDefault(s => s.Name.Equals(assign.StoryName, StringComparison.OrdinalIgnoreCase));
                double elev = story != null ? story.Elevation : 0;
                double botElev = 0;
                if (story != null) {
                    int idx = stories.IndexOf(story);
                    if (idx < stories.Count - 1) botElev = stories[idx + 1].Elevation;
                    else botElev = story.Elevation;
                }

                string section = assign.Section != "DefaultArea" ? assign.Section : "DefaultArea";
                string instanceName = assign.AreaName + "_" + assign.StoryName;
                
                // Classify Wall vs Floor using: 1) Connectivity type, 2) Section PROPTYPE, 3) Point count fallback
                bool isWall = conn.ElementType == "PANEL" || conn.ElementType == "WALL";
                if (!isWall && string.IsNullOrEmpty(conn.ElementType))
                {
                    isWall = sectionPropTypes.ContainsKey(section) && sectionPropTypes[section] == "WALL";
                }
                if (!isWall && string.IsNullOrEmpty(conn.ElementType) && !sectionPropTypes.ContainsKey(section))
                {
                    isWall = conn.PointNames.Count == 2; // Legacy fallback
                }

                if (isWall)
                {
                    walls.Add(new WallData { Name = instanceName, PointNames = new List<string>(conn.PointNames), Section = section, TopElevation = elev, BottomElevation = botElev });
                }
                else
                {
                    floors.Add(new FloorData { Name = instanceName, PointNames = new List<string>(conn.PointNames), Section = section, Elevation = elev });
                }
            }

            // Fallback: areas in connectivity but without any AREAASSIGN
            foreach (var conn in areaConnectivity.Values)
            {
                if (assignedAreas.Contains(conn.Name)) continue;
                if (conn.PointNames.Count < 2) continue;
                
                bool isWall = conn.ElementType == "PANEL" || conn.ElementType == "WALL";
                if (!isWall && string.IsNullOrEmpty(conn.ElementType))
                {
                    isWall = conn.PointNames.Count == 2; // Legacy fallback
                }

                if (isWall)
                {
                    walls.Add(new WallData { Name = conn.Name, PointNames = new List<string>(conn.PointNames), Section = "DefaultArea", TopElevation = 0, BottomElevation = 0 });
                }
                else
                {
                    floors.Add(new FloorData { Name = conn.Name, PointNames = new List<string>(conn.PointNames), Section = "DefaultArea", Elevation = 0 });
                }
            }

            // 5. Parse Point Assigns to update Z elevations
            var ptAssignKeys = tables.Keys.Where(k => k.Contains("POINT ASSIGNS")).ToList();
            foreach (var key in ptAssignKeys)
            {
                foreach (var line in tables[key])
                {
                    var tokens = Tokenize(line);
                    if (tokens.Count >= 3 && tokens[0].Contains("ASSIGN"))
                    {
                        string ptName = Clean(tokens[1]);
                        string storyName = Clean(tokens[2]);
                        if (localPoints.ContainsKey(ptName))
                        {
                            var story = stories.FirstOrDefault(s => s.Name.Equals(storyName, StringComparison.OrdinalIgnoreCase));
                            if (story != null)
                            {
                                var oldPos = localPoints[ptName].Position;
                                localPoints[ptName].Position = new XYZ(oldPos.X, oldPos.Y, story.Elevation);
                            }
                        }
                    }
                }
            }

            points = localPoints.Values.ToList();
        }

        // Phase 1: Connectivity data (geometry only)
        private class FrameConnData
        {
            public string Name { get; set; }
            public string PointI { get; set; }
            public string PointJ { get; set; }
            public bool? IsColumn { get; set; }
        }

        private class AreaConnData
        {
            public string Name { get; set; }
            public List<string> PointNames { get; set; } = new List<string>();
            public string ElementType { get; set; } // "PANEL" = Wall, "FLOOR" = Floor/Slab
        }

        // Phase 2: Per-story assign data
        private class FrameAssignData
        {
            public string FrameName { get; set; }
            public string StoryName { get; set; }
            public string Section { get; set; }
        }

        private class AreaAssignData
        {
            public string AreaName { get; set; }
            public string StoryName { get; set; }
            public string Section { get; set; }
        }

        private static List<string> Tokenize(string line)
        {
            var tokens = new List<string>();
            bool inQuotes = false;
            string current = "";
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    current += c;
                }
                else if (!inQuotes && (c == ' ' || c == '\t' || c == ',' || c == '='))
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current);
                        current = "";
                    }
                }
                else
                {
                    current += c;
                }
            }
            if (current.Length > 0) tokens.Add(current);
            return tokens;
        }

        private string Clean(string input) => input.Replace("\"", "").Trim();

        private double ParseDouble(string input)
        {
            if (double.TryParse(Clean(input), NumberStyles.Any, CultureInfo.InvariantCulture, out double res)) return res;
            return 0;
        }

        private bool TryParseDouble(string input, out double result)
        {
            return double.TryParse(Clean(input), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }
    }
}
