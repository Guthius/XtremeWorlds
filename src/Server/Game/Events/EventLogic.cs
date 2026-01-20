using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Core;
using static Core.Globals.Type;
using static Core.Globals.Commands;
using static Core.Net.Packets;
using System.Reflection.Metadata.Ecma335;
using Core.Globals;
using Core.Net;
using Server.Game;
using Server.Net;
using EventCommand = Core.Globals.EventCommand;

namespace Server
{
    public class EventLogic
    {
        // ******** Enhancements and Explanations ********

        // 1. Asynchronous Operations:
        //    - Changed SpawnGlobalEvents and SpawnAllMapGlobalEvents to be async.  This allows these potentially
        //      long-running operations (especially with large maps and many events) to run without blocking the main thread.
        //    - Used Task.Run where appropriate to offload work to a background thread.
        //    - Added ConfigureAwait(false) to async calls to avoid deadlocks in some contexts.

        // 2. LINQ for Concise Queries:
        //    - Used LINQ (Language Integrated Query) in several places to replace loops with more readable and
        //      often more efficient queries.  This makes the code easier to understand and maintain.

        // 3. Improved Data ures and Error Handling:
        //    - Added null checks and boundary checks to prevent potential `IndexOutOfRangeException` errors.
        //    - Used `?.` (null-conditional operator) and `??` (null-coalescing operator) for safer and more concise null handling.
        //    - Replaced some manual array resizing with `List<T>` and then converted back to arrays when needed. Lists are generally
        //      easier to work with for dynamic resizing.
        //    - Simplified logic in several places by combining conditions and using more direct comparisons.
        //    - Replaced some magic numbers with named constants or enums if they weren't already defined.

        // 4. Code Clarity and Readability:
        //    - Improved code formatting for better readability (consistent indentation, spacing).
        //    - Added comments to explain complex logic sections.
        //    - Replaced some verbose `Conversions.ToBoolean(0)` and `true` with `false` and `true` respectively.
        //    - Replaced some older VB-style string functions (like `InStr`, `Mid`, `Len`, `Val`) with their C# equivalents (`Contains`,
        //      `Substring`, `Length`, `int.Parse` or `double.Parse`).

        // 5. Optimization:
        //    - Cached frequently accessed properties and array lengths to avoid repeated calculations.
        //    - Reduced redundant code by extracting common logic into helper methods.
        //    - Optimized event processing loop by avoiding unnecessary checks and iterations.
        //    - Used StringBuilder for efficient string concatenation in ParseEventText.

        // 6. Event Processing System Enhancements (Conceptual, not fully implemented)
        //    - Added a comment suggesting a possible priority system for events. This wasn't fully implemented,
        //    but it's a good example of a feature enhancement.  A real priority system would probably involve a more sophisticated data structure.

        // 7. Modern C# Features
        //   - Used 'ref' local variables, more directly showing the intent to modify the original structure.
        //   - Used 'var' to declare variables with implicit typing.

        // Constants for enhanced code clarity and maintainability:
        private const int DefaultMovementSpeed = 1; // Example default speed

        // Helper methods for better readability:
        private static bool IsEventVisible(ref MapEvent eventPage) => eventPage.Visible;
        private static int GetEventId(ref MapEvent eventPage) => eventPage.EventId;
        private static EventPage GetEventPage(int map, int eventId, int pageId) => Server.Map.Instance[map].Event[eventId].Pages[pageId];

        public static void RemoveDeadEvents()
        {
            // Use LINQ to iterate through connected players  
            Parallel.ForEach(Enumerable.Range(0, Data.TempPlayer.Length), i =>
            {
                if (Data.TempPlayer[i].EventMap.CurrentEvents > 0 && !Data.TempPlayer[i].GettingMap)
                {
                    int map = GetPlayerMap(i);

                    // Use LINQ to filter and process relevant event pages  
                    var relevantPages = Data.TempPlayer[i].EventMap.EventPages
                        .Where((page, x) => x < Data.TempPlayer[i].EventMap.EventPages.Length)
                        .Where(page => page.EventId < Data.TempPlayer[i].EventMap.CurrentEvents) //Boundary check  
                        .Where(page => map >= 0 && map < Server.Map.Instance.Count && page.EventId < Server.Map.Instance[map].Event.Length) // Boundary check.  
                        .ToList(); // Materialize the query to avoid issues with modifying the collection.  

                    foreach (var eventPage in relevantPages)
                    {
                        int id = eventPage.EventId;
                        int page = eventPage.PageId;

                        // Check if the event and page still exist  
                        if (id >= 0 && map >= 0 && map < Server.Map.Instance.Count &&
                            id < Server.Map.Instance[map].Event.Length &&
                            Server.Map.Instance[map].Event[id].Pages != null &&
                            page >= 0 && page < Server.Map.Instance[map].Event[id].Pages.Length)
                        {
                            ref var playerEventPage = ref Data.TempPlayer[i].EventMap.EventPages[Array.IndexOf(Data.TempPlayer[i].EventMap.EventPages, eventPage)]; //find actual index of eventpage  

                            if (IsEventVisible(ref playerEventPage))
                            {
                                // Check conditions to see if the event should be hidden  
                                EventPage mapEventPage = GetEventPage(map, id, page);

                                if (mapEventPage.ChkHasItem == 1 && Player.HasItem(i, mapEventPage.HasItemIndex) == 0)
                                {
                                    playerEventPage.Visible = false;
                                }

                                if (mapEventPage.ChkSelfSwitch == 1)
                                {
                                    int compare = mapEventPage.SelfSwitchCompare == 0 ? 0 : 1;
                                    bool selfSwitchConditionMet;

                                    if (Server.Map.Instance[map].Event[id].Globals == 1)
                                    {
                                        selfSwitchConditionMet = Server.Map.Instance[map].Event[id].SelfSwitches[mapEventPage.SelfSwitchIndex] == compare;
                                    }
                                    else
                                    {
                                        selfSwitchConditionMet = Data.TempPlayer[i].EventMap.EventPages[id].SelfSwitches[mapEventPage.SelfSwitchIndex] == compare;
                                    }

                                    if (!selfSwitchConditionMet)
                                    {
                                        playerEventPage.Visible = false;
                                    }
                                }

                                if (mapEventPage.ChkVariable == 1)
                                {
                                    int playerVar = Player.Instance[i].Variables[mapEventPage.VariableIndex];
                                    int condition = mapEventPage.VariableCondition;
                                    bool variableConditionMet = false;

                                    switch (mapEventPage.VariableCompare)
                                    {
                                        case 0: variableConditionMet = playerVar == mapEventPage.VariableCondition; break;
                                        case 1: variableConditionMet = playerVar >= mapEventPage.VariableCondition; break;
                                        case 2: variableConditionMet = playerVar <= mapEventPage.VariableCondition; break;
                                        case 3: variableConditionMet = playerVar > mapEventPage.VariableCondition; break;
                                        case 4: variableConditionMet = playerVar < mapEventPage.VariableCondition; break;
                                        case 5: variableConditionMet = playerVar != mapEventPage.VariableCondition; break;
                                    }

                                    if (!variableConditionMet)
                                    {
                                        playerEventPage.Visible = false;
                                    }
                                }

                                if (mapEventPage.ChkSwitch == 1)
                                {
                                    //Simplified with XOR  
                                    if ((mapEventPage.SwitchCompare == 1) ^ (Player.Instance[i].Switches[mapEventPage.SwitchIndex] == 1)) //we are expecting true  
                                    {
                                        playerEventPage.Visible = false;
                                    }
                                }

                                if (Server.Map.Instance[map].Event[id].Globals == 1 && !IsEventVisible(ref playerEventPage))
                                {
                                    Event.TempEventMap[map].Event[id].Active = 0;
                                }

                                if (!IsEventVisible(ref playerEventPage) && id >= 0)
                                {
                                    int pageNum = Array.IndexOf(Server.Map.Instance[map].Event[id].Pages, mapEventPage);
                                    if (pageNum < 0 || pageNum >= Data.TempPlayer[i].EventMap.EventPages.Length)
                                        return;

                                    // Send packet to hide the event  
                                    var packetWriter = new PacketWriter();
                                    packetWriter.WriteEnum(ServerPackets.SSpawnEvent);
                                    packetWriter.WriteInt32(Data.TempPlayer[i].EventMap.CurrentEvents);
                                    packetWriter.WriteInt32(id);
                                    ref var instance = ref Data.TempPlayer[i].EventMap.EventPages[pageNum]; //find actual index of eventpage  
                                    packetWriter.WriteString(Server.Map.Instance[GetPlayerMap(i)].Event[instance.EventId].Name);
                                    packetWriter.WriteByte(instance.Dir);
                                    packetWriter.WriteByte(instance.GraphicType);
                                    packetWriter.WriteInt32(instance.Graphic);
                                    packetWriter.WriteInt32(instance.GraphicX);
                                    packetWriter.WriteInt32(instance.GraphicX2);
                                    packetWriter.WriteInt32(instance.GraphicY);
                                    packetWriter.WriteInt32(instance.GraphicY2);
                                    packetWriter.WriteInt32(instance.MovementSpeed);
                                    packetWriter.WriteInt32(instance.X);
                                    packetWriter.WriteInt32(instance.Y);
                                    packetWriter.WriteByte(instance.Position);
                                    packetWriter.WriteBoolean(instance.Visible);
                                    packetWriter.WriteByte(Server.Map.Instance[map].Event[id].Pages[page].IdleAnim);
                                    packetWriter.WriteByte(Server.Map.Instance[map].Event[id].Pages[page].DirFix);
                                    packetWriter.WriteInt32(Server.Map.Instance[map].Event[id].Pages[page].WalkThrough);
                                    packetWriter.WriteInt32(Server.Map.Instance[map].Event[id].Pages[page].ShowName);

                                    PlayerService.Instance.SendDataTo(i, packetWriter.GetBytes());
                                }
                            }
                        }
                    }
                }
            });
        }

        public static void SpawnNewEvents()
        {
            // Use Parallel.For for potential performance gains on multi-core systems.
            Parallel.ForEach(PlayerService.Instance.PlayerIds, i =>
            {
                int map = GetPlayerMap(i);

                if (Data.TempPlayer[i].EventMap.EventPages != null)
                {
                    // Iterate through the player's current events.  Use a List for easier manipulation.
                    var eventPagesList = Data.TempPlayer[i].EventMap.EventPages.ToList();

                    for (int x = 0; x < eventPagesList.Count; x++)
                    {
                        int p = -1;
                        int id = eventPagesList[x].EventId;

                        // Basic bounds check.
                        if (id < 0 || id >= eventPagesList.Count) continue;

                        int pageId = eventPagesList[x].PageId;

                        if (!eventPagesList[x].Visible)
                            pageId = 0;

                        // Another bounds check.
                        if (Server.Map.Instance[map].Event == null)
                        {
                            break;
                        }

                        if (id >= Server.Map.Instance[map].Event.Length) continue;

                        // Iterate through event pages to find the highest-priority page that meets conditions
                        for (int z = 0; z < Server.Map.Instance[map].Event[id].PageCount; z++)
                        {
                            bool spawnEvent = true;
                            if (Server.Map.Instance[map].Event[id].Pages == null)
                                break;
                            EventPage page = Server.Map.Instance[map].Event[id].Pages[z];

                            // Check conditions (Item, Self Switch, Variable, Switch).
                            if (page.ChkHasItem == 1 && Player.HasItem(i, page.HasItemIndex) == 0)
                            {
                                spawnEvent = false;
                            }

                            if (page.ChkSelfSwitch == 1)
                            {
                                int compare = page.SelfSwitchCompare; // 0 or 1
                                bool selfSwitchStatus;

                                if (Server.Map.Instance[map].Event[id].Globals == 1)
                                    selfSwitchStatus = Server.Map.Instance[map].Event[id].SelfSwitches[page.SelfSwitchIndex] == compare;
                                else
                                    selfSwitchStatus = Data.TempPlayer[i].EventMap.EventPages[id].SelfSwitches[page.SelfSwitchIndex] == compare;

                                if (!selfSwitchStatus)
                                    spawnEvent = false;
                            }


                            if (page.ChkVariable == 1)
                            {
                                int playerVar = Player.Instance[i].Variables[page.VariableIndex];
                                bool conditionMet = false;
                                switch (page.VariableCompare)
                                {
                                    case 0: conditionMet = playerVar == page.VariableCondition; break;
                                    case 1: conditionMet = playerVar >= page.VariableCondition; break;
                                    case 2: conditionMet = playerVar <= page.VariableCondition; break;
                                    case 3: conditionMet = playerVar > page.VariableCondition; break;
                                    case 4: conditionMet = playerVar < page.VariableCondition; break;
                                    case 5: conditionMet = playerVar != page.VariableCondition; break;
                                }

                                if (!conditionMet)
                                    spawnEvent = false;
                            }


                            if (page.ChkSwitch == 1)
                            {
                                // Using XOR for concise switch check.
                                if ((page.SwitchCompare == 0) ^ (Player.Instance[i].Switches[page.SwitchIndex] == 0)) //we want false
                                {
                                    spawnEvent = false; //and switch is true, don't spawn.
                                }
                            }

                            if (spawnEvent)
                            {
                                p = z; // Store the highest-priority valid page index
                            }
                        }

                        // Determine if we should spawn a *new* event (p >= 0 and it wasn't already visible)
                        if (p >= 0 && !eventPagesList[x].Visible)
                        {
                            int z = p;

                            // Reset any active event processing for this event ID.
                            for (int n = 0; n < Data.TempPlayer[i].EventProcessing?.Length; n++)
                            {
                                if (Data.TempPlayer[i].EventProcessing[n].EventId == id)
                                {
                                    Data.TempPlayer[i].EventProcessing[n].EventId = -1;
                                    Data.TempPlayer[i].EventProcessing[n].Active = 0;
                                }
                            }


                            // Set up the event page data.
                            ref var instance = ref Data.TempPlayer[i].EventMap.EventPages[x]; // Use x, as this is the correct index into *this player's* event list
                            EventPage newPage = Server.Map.Instance[map].Event[id].Pages[z];

                            // Respawn should start from the event's spawn location, not wherever it last moved.
                            instance.X = Server.Map.Instance[map].Event[id].X;
                            instance.Y = Server.Map.Instance[map].Event[id].Y;
                            instance.MoveRouteStep = 0;
                            instance.MoveTimer = General.GetTime();

                            // If the event despawned mid-step, clear any active step state.
                            Event.CancelMove(map, x, i, false);

                            instance.Dir = newPage.GraphicType == 1
                                ? (byte)((newPage.GraphicY % 4) switch
                                {
                                    0 => Direction.Down,
                                    1 => Direction.Left,
                                    2 => Direction.Right,
                                    _ => Direction.Up // 3
                                })
                                : (byte)0;

                            instance.Graphic = newPage.Graphic;
                            instance.GraphicType = newPage.GraphicType;
                            instance.GraphicX = newPage.GraphicX;
                            instance.GraphicY = newPage.GraphicY;
                            instance.GraphicX2 = newPage.GraphicX2;
                            instance.GraphicY2 = newPage.GraphicY2;

                            instance.MovementSpeed = newPage.MoveSpeed switch
                            {
                                0 => 2,
                                1 => 3,
                                2 => 4,
                                3 => 6,
                                4 => 12,
                                5 => 24,
                                _ => DefaultMovementSpeed // Handle unexpected values
                            };


                            instance.Position = newPage.Position;
                            instance.EventId = id; // This should be the event ID, not the index in the player's event list.
                            instance.PageId = z;
                            instance.Visible = true;
                            instance.MoveType = newPage.MoveType;

                            if (instance.MoveType == 2) // Custom Move Route
                            {
                                instance.MoveRouteCount = newPage.MoveRouteCount;
                                if (newPage.MoveRouteCount > 0)
                                {
                                    // Copy the move route.
                                    instance.MoveRoute = new MoveRoute[newPage.MoveRouteCount];
                                    Array.Copy(newPage.MoveRoute, instance.MoveRoute, newPage.MoveRouteCount);
                                    instance.MoveRouteComplete = 0; // Ensure it's reset.
                                }
                                else
                                {
                                    instance.MoveRouteComplete = 1; // No route = complete.
                                }
                            }
                            else
                            {
                                instance.MoveRouteComplete = 1;
                            }

                            instance.RepeatMoveRoute = newPage.RepeatMoveRoute;
                            instance.IgnoreIfCannotMove = newPage.IgnoreMoveRoute;
                            instance.MoveFreq = newPage.MoveFreq;
                            instance.MoveSpeed = newPage.MoveSpeed;
                            instance.WalkThrough = newPage.WalkThrough;
                            instance.ShowName = newPage.ShowName;
                            instance.WalkingAnim = newPage.IdleAnim;
                            instance.FixedDir = newPage.DirFix;

                            if (Server.Map.Instance[map].Event[id].Globals == 1)
                            {
                                Event.TempEventMap[map].Event[id].Active = z;
                                Event.TempEventMap[map].Event[id].Position = newPage.Position;

                                // Global events also respawn at their spawn location.
                                Event.TempEventMap[map].Event[id].X = Server.Map.Instance[map].Event[id].X;
                                Event.TempEventMap[map].Event[id].Y = Server.Map.Instance[map].Event[id].Y;
                                Event.TempEventMap[map].Event[id].Dir = instance.Dir;
                                Event.TempEventMap[map].Event[id].MoveRouteStep = 0;
                                Event.TempEventMap[map].Event[id].MoveTimer = General.GetTime();

                                Event.CancelMove(map, id, 0, true);
                            }

                            // Send the spawn event packet.
                            var buffer = new PacketWriter();
                            if (id <= 0)
                                continue;

                            buffer.WriteEnum(ServerPackets.SSpawnEvent);
                            buffer.WriteInt32(Data.TempPlayer[i].EventMap.CurrentEvents);
                            buffer.WriteInt32(id); // Event ID

                            ref var instance1 = ref Data.TempPlayer[i].EventMap.EventPages[x];
                            buffer.WriteString(Server.Map.Instance[map].Event[instance1.EventId].Name);
                            buffer.WriteByte(instance1.Dir);
                            buffer.WriteByte(instance1.GraphicType);
                            buffer.WriteInt32(instance1.Graphic);
                            buffer.WriteInt32(instance1.GraphicX);
                            buffer.WriteInt32(instance1.GraphicX2);
                            buffer.WriteInt32(instance1.GraphicY);
                            buffer.WriteInt32(instance1.GraphicY2);
                            buffer.WriteInt32(instance1.MovementSpeed);
                            buffer.WriteInt32(instance1.X);
                            buffer.WriteInt32(instance1.Y);
                            buffer.WriteByte(instance1.Position);
                            buffer.WriteBoolean(instance1.Visible);
                            buffer.WriteByte(Server.Map.Instance[map].Event[id].Pages[z].IdleAnim);
                            buffer.WriteByte(Server.Map.Instance[map].Event[id].Pages[z].DirFix);
                            buffer.WriteInt32(Server.Map.Instance[map].Event[id].Pages[z].WalkThrough);
                            buffer.WriteInt32(Server.Map.Instance[map].Event[id].Pages[z].ShowName);

                            PlayerService.Instance.SendDataTo(i, buffer.GetBytes());
                        }
                    }
                }
            });
        }

        public static void OnMove()
        {
            // Iterate through all maps.
            for (int i = 0; i < Core.Globals.Variables.MaxMaps; i++)
            {
                // Process global events on this map.
                for (int x = 0; x < Event.TempEventMap[i].EventCount; x++)
                {
                    if (Event.TempEventMap[i].Event[x].Active <= 0) continue;

                    // Don't queue new movement while the event is mid-step.
                    if (Event.IsMoving(i, x, 0, true)) continue;

                    // Check if it's time to process movement.
                    if (Event.TempEventMap[i].Event[x].MoveTimer > General.GetTime()) continue;

                    ref var globalEvent = ref Event.TempEventMap[i].Event[x];

                    // Process movement based on MoveType.
                    switch (globalEvent.MoveType)
                    {
                        case 0: // Fixed, do nothing.
                            break;

                        case 1: // Random Movement
                        {
                            // Direction 0-3 (Up/Down/Left/Right). Adjust max to 3 because NextInt is inclusive.
                            byte rand = (byte)General.GetRandom.NextInt(0, 3); // 0-3 for direction.

                            // Prefer changing direction each tile.
                            if (rand == globalEvent.Dir)
                            {
                                rand = (byte)((rand + General.GetRandom.NextInt(1, 3)) % 4);
                            }
                            if (Event.CanMove(0, i, globalEvent.X, globalEvent.Y, x, globalEvent.WalkThrough, rand, true))
                            {
                                int actualMoveSpeed = globalEvent.MoveSpeed switch
                                {
                                    0 => 2,
                                    1 => 3,
                                    2 => 4,
                                    3 => 6,
                                    4 => 12,
                                    5 => 24,
                                    _ => DefaultMovementSpeed
                                };
                                Event.OnMove(0, i, x, rand, actualMoveSpeed, true);
                            }
                            else
                            {
                                Event.Dir(0, i, x, rand, true); // Just change direction.
                            }

                            break;
                        }
                        case 2: // Custom Move Route
                        {
                            ref var instance = ref Event.TempEventMap[i].Event[x];
                            bool isGlobal = true;
                            int map = i;
                            int playerId = 0;
                            int eventId = x;
                            int walkThrough = instance.WalkThrough;
                            bool doNotProcessMoveRoute = false;

                            if (instance.MoveRouteCount > 0)
                            {
                                if (instance.MoveRouteStep >= instance.MoveRouteCount)
                                {
                                    if (instance.RepeatMoveRoute == 1)
                                    {
                                        instance.MoveRouteStep = 0;
                                        instance.MoveRouteComplete = 1; // Reset for repeating routes.
                                    }
                                    else
                                    {
                                        doNotProcessMoveRoute = true;
                                        instance.MoveRouteComplete = 1; // Mark as complete if not repeating.
                                    }
                                }
                                else //still moving
                                    instance.MoveRouteComplete = 0;


                                if (!doNotProcessMoveRoute)
                                {
                                    instance.MoveRouteStep++;

                                    int actualmovespeed = instance.MoveSpeed switch
                                    {
                                        0 => 2,
                                        1 => 3,
                                        2 => 4,
                                        3 => 6,
                                        4 => 12,
                                        5 => 24,
                                        _ => DefaultMovementSpeed
                                    };


                                    // Get next move route step, handling potential out-of-bounds access.
                                    if (instance.MoveRouteStep < 0 || instance.MoveRouteStep >= instance.MoveRoute.Length)
                                    {
                                        //Error, route step out of bounds
                                        break;
                                    }

                                    var nextMove = instance.MoveRoute[instance.MoveRouteStep];


                                    bool sendUpdate = false;
                                    switch (nextMove.Index)
                                    {
                                        case 1: // Move Up
                                            if (Event.CanMove(playerId, map, instance.X, instance.Y, eventId, walkThrough, (byte) Direction.Up, isGlobal))
                                            {
                                                Event.OnMove(playerId, map, eventId, (byte)Direction.Up, actualmovespeed, isGlobal);
                                            }
                                            else if (instance.IgnoreIfCannotMove == 0)
                                            {
                                                instance.MoveRouteStep--;
                                            }

                                            break;
                                        case 2: // Move Down
                                            if (Event.CanMove(playerId, map, instance.X, instance.Y, eventId, walkThrough, (byte) Direction.Down, isGlobal))
                                            {
                                                Event.OnMove(playerId, map, eventId, (byte)Direction.Down, actualmovespeed, isGlobal);
                                            }
                                            else if (instance.IgnoreIfCannotMove == 0)
                                            {
                                                instance.MoveRouteStep--;
                                            }

                                            break;
                                        case 3: // Move Left
                                            if (Event.CanMove(playerId, map, instance.X, instance.Y, eventId, walkThrough, (byte) Direction.Left, isGlobal))
                                            {
                                                Event.OnMove(playerId, map, eventId, (byte)Direction.Left, actualmovespeed, isGlobal);
                                            }
                                            else if (instance.IgnoreIfCannotMove == 0)
                                            {
                                                instance.MoveRouteStep--;
                                            }

                                            break;
                                        case 4: // Move Right
                                            if (Event.CanMove(playerId, map, instance.X, instance.Y, eventId, walkThrough, (byte) Direction.Right, isGlobal))
                                            {
                                                Event.OnMove(playerId, map, eventId, (byte)Direction.Right, actualmovespeed, isGlobal);
                                            }
                                            else if (instance.IgnoreIfCannotMove == 0)
                                            {
                                                instance.MoveRouteStep--;
                                            }

                                            break;
                                        case 5: // Move Random
                                        {
                                            byte z = (byte)General.GetRandom.NextInt(0, 4); // 0-3
                                            if (Event.CanMove(playerId, map, instance.X, instance.Y, eventId, walkThrough, z, isGlobal))
                                            {
                                                Event.OnMove(playerId, map, eventId, z, actualmovespeed, isGlobal);
                                            }
                                            else if (instance.IgnoreIfCannotMove == 0)
                                            {
                                                instance.MoveRouteStep--;
                                            }

                                            break;
                                        }

                                        case 6: // Move Toward Player
                                        {
                                            if (!isGlobal) //should never be global.
                                            {
                                                // Determine if the event is one block away from the player.
                                                if (Event.IsOneBlockAway(instance.X, instance.Y, GetPlayerX(playerId), GetPlayerY(playerId)))
                                                {
                                                    // Face the player.
                                                    Event.Dir(playerId, GetPlayerMap(playerId), eventId, (byte)Event.GetDirToPlayer(playerId, GetPlayerMap(playerId), eventId), false);
                                                    if (instance.IgnoreIfCannotMove == 0)
                                                    {
                                                        instance.MoveRouteStep--;
                                                    }
                                                }
                                                else
                                                {
                                                    // Try to move towards the player.
                                                    byte z = Event.CanMoveTowardsPlayer(playerId, map, eventId);
                                                    if (z < 4) // Valid direction (0-3).
                                                    {
                                                        if (Event.CanMove(playerId, map, instance.X, instance.Y, eventId, walkThrough, z, isGlobal))
                                                        {
                                                            Event.OnMove(playerId, map, eventId, z, actualmovespeed, isGlobal);
                                                        }
                                                        else if (instance.IgnoreIfCannotMove == 0)
                                                        {
                                                            instance.MoveRouteStep--;
                                                        }
                                                    }
                                                    else if (instance.IgnoreIfCannotMove == 0) // Cannot move towards player and we don't ignore.
                                                    {
                                                        instance.MoveRouteStep--;
                                                    }
                                                }
                                            }

                                            break;
                                        }

                                        case 7: // Move Away from Player
                                        {
                                            if (!isGlobal)
                                            {
                                                int z = Event.CanMoveAwayFromPlayer(playerId, map, eventId);
                                                if (z < 5)
                                                {
                                                    if (Event.CanMove(playerId, map, instance.X, instance.Y, eventId, walkThrough, (byte) z, isGlobal))
                                                    {
                                                        Event.OnMove(playerId, map, eventId, (byte)z, actualmovespeed, isGlobal);
                                                    }
                                                    else if (instance.IgnoreIfCannotMove == 0)
                                                    {
                                                        instance.MoveRouteStep--;
                                                    }
                                                }
                                            }

                                            break;
                                        }

                                        case 8: // Move Forward
                                            if (Event.CanMove(playerId, map, instance.X, instance.Y, eventId, walkThrough, (byte) instance.Dir, isGlobal))
                                            {
                                                Event.OnMove(playerId, map, eventId, (byte)instance.Dir, actualmovespeed, isGlobal);
                                            }
                                            else if (instance.IgnoreIfCannotMove == 0)
                                            {
                                                instance.MoveRouteStep--;
                                            }

                                            break;
                                        case 9: // Move Backward
                                        {
                                            byte z = instance.Dir switch
                                            {
                                                (byte) Direction.Up => (byte) Direction.Down,
                                                (byte) Direction.Down => (byte) Direction.Up,
                                                (byte) Direction.Left => (byte) Direction.Right,
                                                (byte) Direction.Right => (byte) Direction.Left,
                                                _ => instance.Dir // Invalid direction, keep current.
                                            };
                                            if (Event.CanMove(playerId, map, instance.X, instance.Y, eventId, walkThrough, z, isGlobal))
                                            {
                                                Event.OnMove(playerId, map, eventId, z, actualmovespeed, isGlobal);
                                            }
                                            else if (instance.IgnoreIfCannotMove == 0)
                                            {
                                                instance.MoveRouteStep--;
                                            }

                                            break;
                                        }

                                        case 10: instance.MoveTimer = General.GetTime() + 100; break;
                                        case 11: instance.MoveTimer = General.GetTime() + 500; break;
                                        case 12: instance.MoveTimer = General.GetTime() + 1000; break;

                                        case 13: Event.Dir(playerId, map, eventId, (byte) Direction.Up, isGlobal); break;
                                        case 14: Event.Dir(playerId, map, eventId, (byte) Direction.Down, isGlobal); break;
                                        case 15: Event.Dir(playerId, map, eventId, (byte) Direction.Left, isGlobal); break;
                                        case 16: Event.Dir(playerId, map, eventId, (byte) Direction.Right, isGlobal); break;

                                        // Turn 90 degrees clockwise, counter-clockwise, 180 degrees, or at random
                                        case 17: // Turn Right 90 Degrees
                                        {
                                            byte z = instance.Dir switch
                                            {
                                                (byte) Direction.Up => (byte) Direction.Right,
                                                (byte) Direction.Right => (byte) Direction.Down,
                                                (byte) Direction.Left => (byte) Direction.Up,
                                                (byte) Direction.Down => (byte) Direction.Left,
                                                _ => instance.Dir
                                            };
                                            Event.Dir(playerId, map, eventId, z, isGlobal);
                                            break;
                                        }
                                        case 18: // Turn Left 90 Degrees
                                        {
                                            byte z = instance.Dir switch
                                            {
                                                (byte) Direction.Up => (byte) Direction.Left,
                                                (byte) Direction.Right => (byte) Direction.Up,
                                                (byte) Direction.Left => (byte) Direction.Down,
                                                (byte) Direction.Down => (byte) Direction.Right,
                                                _ => instance.Dir
                                            };
                                            Event.Dir(playerId, map, eventId, z, isGlobal);
                                            break;
                                        }
                                        case 19: // Turn 180 Degrees
                                        {
                                            byte z = instance.Dir switch
                                            {
                                                (byte) Direction.Up => (byte) Direction.Down,
                                                (byte) Direction.Right => (byte) Direction.Left,
                                                (byte) Direction.Left => (byte) Direction.Right,
                                                (byte) Direction.Down => (byte) Direction.Up,
                                                _ => instance.Dir
                                            };
                                            Event.Dir(playerId, map, eventId, z, isGlobal);
                                            break;
                                        }
                                        case 20: // Turn Random
                                        {
                                            byte z = (byte)General.GetRandom.NextInt(0, 4);
                                            Event.Dir(playerId, map, eventId, z, isGlobal);
                                            break;
                                        }
                                        case 21: // Turn Toward Player
                                        {
                                            if (!isGlobal)
                                            {
                                                byte z = (byte)Event.GetDirToPlayer(playerId, map, eventId);
                                                Event.Dir(playerId, map, eventId, z, isGlobal);
                                            }

                                            break;
                                        }

                                        case 22: // Turn Away from Player
                                        {
                                            if (!isGlobal)
                                            {
                                                byte z = (byte)Event.GetDirAwayFromPlayer(playerId, map, eventId);
                                                Event.Dir(playerId, map, eventId, z, isGlobal);
                                            }

                                            break;
                                        }

                                        // Change Speed, Frequency, Graphic
                                        case 23: instance.MoveSpeed = 0; break;
                                        case 24: instance.MoveSpeed = 1; break;
                                        case 25: instance.MoveSpeed = 2; break;
                                        case 26: instance.MoveSpeed = 3; break;
                                        case 27: instance.MoveSpeed = 4; break;
                                        case 28: instance.MoveSpeed = 5; break;

                                        case 29: instance.MoveFreq = 0; break;
                                        case 30: instance.MoveFreq = 1; break;
                                        case 31: instance.MoveFreq = 2; break;
                                        case 32: instance.MoveFreq = 3; break;
                                        case 33: instance.MoveFreq = 4; break;

                                        case 34: // Turn On Walking Animation
                                            instance.WalkingAnim = 1;
                                            sendUpdate = true;
                                            break;
                                        case 35: // Turn Off Walking Animation
                                            instance.WalkingAnim = 0;
                                            sendUpdate = true;
                                            break;

                                        case 36: // Turn On Direction Fix
                                            instance.FixedDir = 1;
                                            sendUpdate = true;
                                            break;
                                        case 37: // Turn Off Direction Fix
                                            instance.FixedDir = 0;
                                            sendUpdate = true;
                                            break;

                                        case 38: // Turn On Through
                                            instance.WalkThrough = 1;
                                            break;
                                        case 39: // Turn Off Through
                                            instance.WalkThrough = 0;
                                            break;
                                        // Event draw priority (0=Below Player, 1=Same as Player, 2=Above Player)
                                        case 40: // Below Player
                                            instance.Position = 0;
                                            sendUpdate = true;
                                            break;
                                        case 41: // Same as Player
                                            instance.Position = 1;
                                            sendUpdate = true;
                                            break;
                                        case 42: // Above Player
                                            instance.Position = 2;
                                            sendUpdate = true;
                                            break;

                                        case 43: // Change Graphic
                                        {
                                            instance.GraphicType = (byte) nextMove.Data1;
                                            instance.Graphic = nextMove.Data2;
                                            instance.GraphicX = nextMove.Data3;
                                            instance.GraphicX2 = nextMove.Data4;
                                            instance.GraphicY = nextMove.Data5;
                                            instance.GraphicY2 = nextMove.Data6;

                                            // Adjust direction if it's a character graphic.
                                            if (instance.GraphicType == 1)
                                            {
                                                instance.Dir = instance.GraphicY switch
                                                {
                                                    0 => (int) Direction.Down,
                                                    1 => (int) Direction.Left,
                                                    2 => (int) Direction.Right,
                                                    3 => (int) Direction.Up,
                                                    _ => instance.Dir
                                                };
                                            }

                                            sendUpdate = true;
                                            break;
                                        }
                                    }


                                    if (sendUpdate)
                                    {
                                        var buffer = new PacketWriter();
                                        {
                                            buffer.WriteEnum(ServerPackets.SSpawnEvent);
                                            buffer.WriteInt32(Data.TempPlayer[i].EventMap.CurrentEvents);
                                            buffer.WriteInt32(eventId);

                                            ref var instance1 = ref Event.TempEventMap[i].Event[x];
                                            buffer.WriteString(Server.Map.Instance[i].Event[x].Name); // Global event, use map index
                                            buffer.WriteByte(instance1.Dir);
                                            buffer.WriteByte(instance1.GraphicType);
                                            buffer.WriteInt32(instance1.Graphic);
                                            buffer.WriteInt32(instance1.GraphicX);
                                            buffer.WriteInt32(instance1.GraphicX2);
                                            buffer.WriteInt32(instance1.GraphicY);
                                            buffer.WriteInt32(instance1.GraphicY2);
                                            buffer.WriteByte(instance1.MoveSpeed);
                                            buffer.WriteInt32(instance1.X);
                                            buffer.WriteInt32(instance1.Y);
                                            buffer.WriteByte(instance1.Position);
                                            buffer.WriteBoolean(instance1.Active != 0);
                                            buffer.WriteInt32(instance1.WalkingAnim); // Corrected property names
                                            buffer.WriteInt32(instance1.FixedDir);
                                            buffer.WriteInt32(instance1.WalkThrough);
                                            buffer.WriteInt32(instance1.ShowName);
                                            NetworkConfig.SendDataToMap(i, buffer.GetBytes());
                                        }
                                    }
                                }

                                doNotProcessMoveRoute = false; // Reset for next iteration.
                            }

                            break;
                        }
                    }

                    // Set the next move timer based on MoveFreq.
                    globalEvent.MoveTimer = General.GetTime() + globalEvent.MoveFreq switch
                    {
                        0 => 4000,
                        1 => 2000,
                        2 => 1000,
                        3 => 500,
                        4 => 250,
                        _ => 1000 // Default if invalid.
                    };
                }
            }
        }


        public static void ProcessLocalMovement()
        {
            // Parallel processing for each player.
            Parallel.ForEach(PlayerService.Instance.PlayerIds, i =>
            {
                try
                {
                    if (Data.TempPlayer == null || i < 0 || i >= Data.TempPlayer.Length)
                        return;

                    // Skip non-playing or half-disconnected players.
                    if (!Data.TempPlayer[i].InGame)
                        return;

                    int map = GetPlayerMap(i);
                    if (map < 0 || Server.Map.Instance == null || map >= Server.Map.Instance.Count)
                        return;
                    if (Server.Map.Instance[map].Event == null)
                        return;

                    if (Data.TempPlayer[i].EventMap.EventPages == null)
                        return;
                    if (Data.TempPlayer[i].EventMap.CurrentEvents <= 0)
                        return;

                    // Iterate through local events for the player.
                    for (int x = 0; x < Data.TempPlayer[i].EventMap.CurrentEvents; x++)
                    {
                        if (x >= Data.TempPlayer[i].EventMap.EventPages.Length)
                            break;

                        // Bounds check.
                        int mapEventId = Data.TempPlayer[i].EventMap.EventPages[x].EventId;
                        if (mapEventId < 0 || mapEventId >= Server.Map.Instance[map].Event.Length) continue;

                        ref var localEvent = ref Data.TempPlayer[i].EventMap.EventPages[x];

                        // Only process visible, non-global events.
                        if (Server.Map.Instance[map].Event[mapEventId].Globals != 0 || !localEvent.Visible) continue;

                        // Don't queue new movement while the event is mid-step.
                        if (Event.IsMoving(map, x, i, false)) continue;

                        // Check move timer.
                        if (localEvent.MoveTimer > General.GetTime()) continue;

                        // Process movement based on MoveType.
                        switch (localEvent.MoveType)
                        {
                            case 0: // Fixed
                                break;

                            case 1: // Random
                            {
                                // Direction 0-3 inclusive (NextInt inclusive upper bound).
                                byte rand = (byte)General.GetRandom.NextInt(0, 3);
                                if (Event.CanMove(i, map, localEvent.X, localEvent.Y, x, localEvent.WalkThrough, (byte) rand, false))
                                {
                                    int actualMoveSpeed = localEvent.MoveSpeed switch
                                    {
                                        0 => 2,
                                        1 => 3,
                                        2 => 4,
                                        3 => 6,
                                        4 => 12,
                                        5 => 24,
                                        _ => DefaultMovementSpeed
                                    };
                                    Event.OnMove(i, map, x, rand, actualMoveSpeed, false);
                                }
                                else
                                {
                                    Event.Dir(i, map, x, rand, false);
                                }

                                break;
                            }
                            case 2: // Custom Move Route
                            {
                                ref var instance = ref Data.TempPlayer[i].EventMap.EventPages[x];
                                bool isGlobal = false;
                                bool sendUpdate = false;
                                int eventId = x;
                                int walkThrough = instance.WalkThrough;
                                bool doNotProcessMoveRoute = false;

                                if (instance.MoveRouteCount > 0)
                                {
                                    if (instance.MoveRouteStep >= instance.MoveRouteCount)
                                    {
                                        if (instance.RepeatMoveRoute == 1)
                                        {
                                            instance.MoveRouteStep = 0;
                                            instance.MoveRouteComplete = 1; // Reset for repeating.
                                        }
                                        else
                                        {
                                            doNotProcessMoveRoute = true;
                                            instance.MoveRouteComplete = 1; // Mark as complete.
                                        }
                                    }
                                    else //still moving
                                        instance.MoveRouteComplete = 0;


                                    if (!doNotProcessMoveRoute)
                                    {
                                        instance.MoveRouteStep++;

                                        int actualmovespeed = instance.MoveSpeed switch
                                        {
                                            0 => 2,
                                            1 => 3,
                                            2 => 4,
                                            3 => 6,
                                            4 => 12,
                                            5 => 24,
                                            _ => DefaultMovementSpeed
                                        };


                                        // Get next move route step, handling potential out-of-bounds access.
                                        if (instance.MoveRouteStep < 0 || instance.MoveRouteStep >= instance.MoveRoute.Length)
                                        {
                                            //error, route step out of range
                                            break; // Exit the switch statement.
                                        }

                                        var nextMove = instance.MoveRoute[instance.MoveRouteStep];

                                        switch (nextMove.Index)
                                        {
                                            case 1: // Move Up
                                                if (Event.CanMove(i, map, instance.X, instance.Y, eventId, walkThrough, (byte) Direction.Up, isGlobal))
                                                {
                                                    Event.OnMove(i, map, eventId, (byte)Direction.Up, actualmovespeed, isGlobal);
                                                }
                                                else if (instance.IgnoreIfCannotMove == 0)
                                                {
                                                    instance.MoveRouteStep--;
                                                }

                                                break;
                                            case 2: // Move Down
                                                if (Event.CanMove(i, map, instance.X, instance.Y, eventId, walkThrough, (byte) Direction.Down, isGlobal))
                                                {
                                                    Event.OnMove(i, map, eventId, (byte)Direction.Down, actualmovespeed, isGlobal);
                                                }
                                                else if (instance.IgnoreIfCannotMove == 0)
                                                {
                                                    instance.MoveRouteStep--;
                                                }

                                                break;
                                            case 3: // Move Left
                                                if (Event.CanMove(i, map, instance.X, instance.Y, eventId, walkThrough, (byte) Direction.Left, isGlobal))
                                                {
                                                    Event.OnMove(i, map, eventId, (byte)Direction.Left, actualmovespeed, isGlobal);
                                                }
                                                else if (instance.IgnoreIfCannotMove == 0)
                                                {
                                                    instance.MoveRouteStep--;
                                                }

                                                break;
                                            case 4: // Move Right
                                                if (Event.CanMove(i, map, instance.X, instance.Y, eventId, walkThrough, (byte) Direction.Right, isGlobal))
                                                {
                                                    Event.OnMove(i, map, eventId, (byte)Direction.Right, actualmovespeed, isGlobal);
                                                }
                                                else if (instance.IgnoreIfCannotMove == 0)
                                                {
                                                    instance.MoveRouteStep--;
                                                }

                                                break;
                                            case 5: // Move Random
                                            {
                                                byte z = (byte)General.GetRandom.NextInt(0, 4);
                                                if (Event.CanMove(i, map, instance.X, instance.Y, eventId, walkThrough, (byte) z, isGlobal))
                                                {
                                                    Event.OnMove(i, map, eventId, z, actualmovespeed, isGlobal);
                                                }
                                                else if (instance.IgnoreIfCannotMove == 0)
                                                {
                                                    instance.MoveRouteStep--;
                                                }

                                                break;
                                            }

                                            case 6: // Move Toward Player
                                            {
                                                if (!isGlobal)
                                                {
                                                    if (Event.IsOneBlockAway(instance.X, instance.Y, GetPlayerX(i), GetPlayerY(i)))
                                                    {
                                                        Event.Dir(i, map, eventId, (byte)Event.GetDirToPlayer(i, map, eventId), false);

                                                        // Activate event if triggered by player action.
                                                        int mapEventId2 = Data.TempPlayer[i].EventMap.EventPages[eventId].EventId;
                                                        if (mapEventId2 >= 0 && mapEventId2 < Server.Map.Instance[map].Event.Length &&
                                                            Server.Map.Instance[map].Event[mapEventId2].Pages[Data.TempPlayer[i].EventMap.EventPages[eventId].PageId].Trigger == 1)
                                                        {
                                                            if (Server.Map.Instance[map].Event[mapEventId2].Pages[Data.TempPlayer[i].EventMap.EventPages[eventId].PageId].CommandListCount > 0)
                                                            {
                                                                // Start event processing.
                                                                ref var eventProcessing = ref Data.TempPlayer[i].EventProcessing[eventId]; // Use EventId (local index)
                                                                eventProcessing.Active = 1;
                                                                eventProcessing.ActionTimer = General.GetTime();
                                                                eventProcessing.CurList = 0;
                                                                eventProcessing.CurSlot = 0;
                                                                eventProcessing.EventId = mapEventId2; // Map event ID
                                                                eventProcessing.PageId = Data.TempPlayer[i].EventMap.EventPages[eventId].PageId; // Local page ID.
                                                                eventProcessing.WaitingForResponse = 0;
                                                                eventProcessing.ListLeftOff = new int[Server.Map.Instance[map].Event[mapEventId2].Pages[eventProcessing.PageId].CommandListCount];
                                                                Array.Fill(eventProcessing.ListLeftOff, -1);
                                                            }
                                                        }

                                                        if (instance.IgnoreIfCannotMove == 0)
                                                        {
                                                            instance.MoveRouteStep--;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        int z = Event.CanMoveTowardsPlayer(i, map, eventId);
                                                        if (z < 4)
                                                        {
                                                            if (Event.CanMove(i, map, instance.X, instance.Y, eventId, walkThrough, (byte) z, isGlobal))
                                                            {
                                                                Event.OnMove(i, map, eventId, (byte)z, actualmovespeed, isGlobal);
                                                            }
                                                            else if (instance.IgnoreIfCannotMove == 0)
                                                            {
                                                                instance.MoveRouteStep--;
                                                            }
                                                        }
                                                        else if (instance.IgnoreIfCannotMove == 0)
                                                        {
                                                            instance.MoveRouteStep = instance.MoveRouteStep - 1;
                                                        }
                                                    }
                                                }

                                                break;
                                            }
                                            case 7: // Move Away From Player
                                            {
                                                if (!isGlobal)
                                                {
                                                    int z = Event.CanMoveAwayFromPlayer(i, map, eventId);
                                                    if (z < 5) // Valid direction.
                                                    {
                                                        if (Event.CanMove(i, map, instance.X, instance.Y, eventId, walkThrough, (byte) z, isGlobal))
                                                        {
                                                            Event.OnMove(i, map, eventId, (byte)z, actualmovespeed, isGlobal);
                                                        }
                                                        else if (instance.IgnoreIfCannotMove == 0)
                                                        {
                                                            instance.MoveRouteStep--;
                                                        }
                                                    }
                                                }

                                                break;
                                            }
                                            case 8: // Move Forward
                                                if (Event.CanMove(i, map, instance.X, instance.Y, eventId, walkThrough, (byte) instance.Dir, isGlobal))
                                                {
                                                    Event.OnMove(i, map, eventId, instance.Dir, actualmovespeed, isGlobal);
                                                }
                                                else if (instance.IgnoreIfCannotMove == 0)
                                                {
                                                    instance.MoveRouteStep--;
                                                }

                                                break;

                                            case 9: // Move Backward
                                            {
                                                byte z = instance.Dir switch
                                                {
                                                    (byte) Direction.Up => (byte) Direction.Down,
                                                    (byte) Direction.Down => (byte) Direction.Up,
                                                    (byte) Direction.Left => (byte) Direction.Right,
                                                    (byte) Direction.Right => (byte) Direction.Left,
                                                    _ => instance.Dir
                                                };

                                                if (Event.CanMove(i, map, instance.X, instance.Y, eventId, walkThrough, (byte) z, isGlobal))
                                                {
                                                    Event.OnMove(i, map, eventId, z, actualmovespeed, isGlobal);
                                                }
                                                else if (instance.IgnoreIfCannotMove == 0)
                                                {
                                                    instance.MoveRouteStep--;
                                                }

                                                break;
                                            }
                                            case 10: instance.MoveTimer = General.GetTime() + 100; break;
                                            case 11: instance.MoveTimer = General.GetTime() + 500; break;
                                            case 12: instance.MoveTimer = General.GetTime() + 1000; break;

                                            case 13: Event.Dir(i, map, eventId, (byte) Direction.Up, isGlobal); break;
                                            case 14: Event.Dir(i, map, eventId, (byte) Direction.Down, isGlobal); break;
                                            case 15: Event.Dir(i, map, eventId, (byte) Direction.Left, isGlobal); break;
                                            case 16: Event.Dir(i, map, eventId, (byte) Direction.Right, isGlobal); break;

                                            // Turn 90 degrees clockwise, counter-clockwise, 180 degrees
                                            case 17: // Turn Right 90 Degrees
                                            {
                                                byte z = instance.Dir switch
                                                {
                                                    (byte) Direction.Up => (byte) Direction.Right,
                                                    (byte) Direction.Right => (byte) Direction.Down,
                                                    (byte) Direction.Left => (byte) Direction.Up,
                                                    (byte) Direction.Down => (byte) Direction.Left,
                                                    _ => instance.Dir
                                                };
                                                Event.Dir(i, map, eventId, z, isGlobal);
                                                break;
                                            }
                                            case 18: // Turn Left 90 Degrees
                                            {
                                                byte z = instance.Dir switch
                                                {
                                                    (byte) Direction.Up => (byte) Direction.Left,
                                                    (byte) Direction.Right => (byte) Direction.Up,
                                                    (byte) Direction.Left => (byte) Direction.Down,
                                                    (byte) Direction.Down => (byte) Direction.Right,
                                                    _ => instance.Dir
                                                };
                                                Event.Dir(i, map, eventId, z, isGlobal);
                                                break;
                                            }
                                            case 19: // Turn 180 Degrees
                                            {
                                                byte z = instance.Dir switch
                                                {
                                                    (byte) Direction.Up => (byte) Direction.Down,
                                                    (byte) Direction.Right => (byte) Direction.Left,
                                                    (byte) Direction.Left => (byte) Direction.Right,
                                                    (byte) Direction.Down => (byte) Direction.Up,
                                                    _ => instance.Dir
                                                };
                                                Event.Dir(i, map, eventId, z, isGlobal);
                                                break;
                                            }
                                            case 20: // Turn Random
                                            {
                                                byte z = (byte)General.GetRandom.NextInt(0, 4);
                                                Event.Dir(i, map, eventId, z, isGlobal);
                                                break;
                                            }
                                            case 21: // Turn Toward Player
                                            {
                                                if (!isGlobal)
                                                {
                                                    int z = Event.GetDirToPlayer(i, map, eventId);
                                                    Event.Dir(i, map, eventId, (byte)z, isGlobal);
                                                }

                                                break;
                                            }
                                            case 22: // Turn Away from Player
                                            {
                                                if (!isGlobal)
                                                {
                                                    int z = Event.GetDirAwayFromPlayer(i, map, eventId);
                                                    Event.Dir(i, map, eventId, (byte)z, isGlobal);
                                                }

                                                break;
                                            }

                                            // Change Speed, Frequency, Graphic
                                            case 23: instance.MoveSpeed = 0; break;
                                            case 24: instance.MoveSpeed = 1; break;
                                            case 25: instance.MoveSpeed = 2; break;
                                            case 26: instance.MoveSpeed = 3; break;
                                            case 27: instance.MoveSpeed = 4; break;
                                            case 28: instance.MoveSpeed = 5; break;

                                            case 29: instance.MoveFreq = 0; break;
                                            case 30: instance.MoveFreq = 1; break;
                                            case 31: instance.MoveFreq = 2; break;
                                            case 32: instance.MoveFreq = 3; break;
                                            case 33: instance.MoveFreq = 4; break;

                                            case 34:
                                                instance.WalkingAnim = 1;
                                                sendUpdate = true;
                                                break; // Turn On Walking Animation
                                            case 35:
                                                instance.WalkingAnim = 0;
                                                sendUpdate = true;
                                                break; // Turn Off Walking Animation
                                            case 36:
                                                instance.FixedDir = 1;
                                                sendUpdate = true;
                                                break; // Turn On Direction Fix
                                            case 37:
                                                instance.FixedDir = 0;
                                                sendUpdate = true;
                                                break; // Turn Off Direction Fix
                                            case 38: instance.WalkThrough = 1; break; // Turn On Through
                                            case 39: instance.WalkThrough = 0; break; // Turn Off Through
                                            // Event draw priority (0=Below Player, 1=Same as Player, 2=Above Player)
                                            case 40: // Below Player
                                                instance.Position = 0;
                                                sendUpdate = true;
                                                break;
                                            case 41: // Same as Player
                                                instance.Position = 1;
                                                sendUpdate = true;
                                                break;
                                            case 42: // Above Player
                                                instance.Position = 2;
                                                sendUpdate = true;
                                                break;

                                            case 43: // Change Graphic
                                            {
                                                instance.GraphicType = (byte) nextMove.Data1;
                                                instance.Graphic = nextMove.Data2;
                                                instance.GraphicX = nextMove.Data3;
                                                instance.GraphicX2 = nextMove.Data4;
                                                instance.GraphicY = nextMove.Data5;
                                                instance.GraphicY2 = nextMove.Data6;

                                                // Adjust direction if it's a character graphic.
                                                if (instance.GraphicType == 1)
                                                {
                                                    instance.Dir = instance.GraphicY switch
                                                    {
                                                        0 => (int) Direction.Down,
                                                        1 => (int) Direction.Left,
                                                        2 => (int) Direction.Right,
                                                        3 => (int) Direction.Up,
                                                        _ => instance.Dir
                                                    };
                                                }

                                                sendUpdate = true;
                                                break;
                                            }
                                        }

                                        // Send update if necessary.
                                        if (sendUpdate && Data.TempPlayer[i].EventMap.EventPages[eventId].EventId >= 0)
                                        {
                                            var buffer = new PacketWriter();
                                            buffer.WriteEnum(ServerPackets.SSpawnEvent);
                                            buffer.WriteInt32(Data.TempPlayer[i].EventMap.CurrentEvents);
                                            buffer.WriteInt32(Data.TempPlayer[i].EventMap.EventPages[eventId].EventId); // Use map event ID

                                            ref var instance1 = ref Data.TempPlayer[i].EventMap.EventPages[eventId];
                                            buffer.WriteString(Server.Map.Instance[map].Event[instance1.EventId].Name); //use map event Id
                                            buffer.WriteByte(instance1.Dir);
                                            buffer.WriteByte(instance1.GraphicType);
                                            buffer.WriteInt32(instance1.Graphic);
                                            buffer.WriteInt32(instance1.GraphicX);
                                            buffer.WriteInt32(instance1.GraphicX2);
                                            buffer.WriteInt32(instance1.GraphicY);
                                            buffer.WriteInt32(instance1.GraphicY2);
                                            buffer.WriteInt32(instance1.MovementSpeed); // Use consistent naming
                                            buffer.WriteInt32(instance1.X);
                                            buffer.WriteInt32(instance1.Y);
                                            buffer.WriteByte(instance1.Position);
                                            buffer.WriteBoolean(instance1.Visible);
                                            buffer.WriteInt32(instance1.WalkingAnim);
                                            buffer.WriteInt32(instance1.FixedDir);
                                            buffer.WriteInt32(instance1.WalkThrough);
                                            buffer.WriteInt32(instance1.ShowName);
                                            PlayerService.Instance.SendDataTo(i, buffer.GetBytes());
                                        }
                                    }

                                    doNotProcessMoveRoute = false; // Reset for the next loop iteration.
                                }

                                break;
                            }
                        }

                        // Set next move timer based on MoveFreq.
                        localEvent.MoveTimer = General.GetTime() + localEvent.MoveFreq switch
                        {
                            0 => 4000,
                            1 => 2000,
                            2 => 1000,
                            3 => 500,
                            4 => 250,
                            _ => 1000
                        };
                    }
                }
                catch (Exception ex)
                {
                    General.Logger.LogError(ex, "ProcessLocalMovement crashed for player {PlayerId}", i);
                }
            });
        }

        public static void ProcessEventCommands()
        {
            // Snapshot player ids to avoid enumerating a mutable LinkedList while running in parallel.
            var playerIds = PlayerService.Instance.PlayerIds.ToArray();

            // Parallel processing for each player.
            Parallel.ForEach(playerIds, i =>
            {
                try
                {
                    if (i < 0 || i >= Data.TempPlayer.Length) return;
                    if (!NetworkConfig.IsPlaying(i) || Data.TempPlayer[i].GettingMap || Data.TempPlayer[i].EventMap.CurrentEvents <= 0) return;

                    if (i < 0 || Player.Instance == null || i >= Player.Instance.Count) return;
                    int map = Player.Instance[i].Map; // Cache map number.

                    // Iterate through the player's events.
                    for (int x = 0; x < Data.TempPlayer[i].EventMap.CurrentEvents; x++)
                    {
                        if (x >= Data.TempPlayer[i].EventMap.EventPages.Length)
                            break;

                        if (Data.TempPlayer[i].EventProcessingCount <= 0) continue;

                        ref var eventPage = ref Data.TempPlayer[i].EventMap.EventPages[x];

                        if (!eventPage.Visible) continue;

                        // Check event and page validity.
                        if (eventPage.EventId >= Server.Map.Instance[map].Event.Length || Server.Map.Instance[map].Event == null || Server.Map.Instance[map].Event[eventPage.EventId].Pages == null || eventPage.PageId >= Server.Map.Instance[map].Event[eventPage.EventId].Pages.Length) continue;

                        // Handle parallel process events (Trigger == 2).
                        if (Server.Map.Instance[map].Event[eventPage.EventId].Pages[eventPage.PageId].Trigger == 2)
                        {
                            // If not already active, start the event processing.
                            if (Data.TempPlayer[i].EventProcessing[eventPage.EventId].Active == 0) // Use map event ID for indexing.
                            {
                                if (Server.Map.Instance[map].Event[eventPage.EventId].Pages[eventPage.PageId].CommandListCount > 0)
                                {
                                    ref var eventProcessing = ref Data.TempPlayer[i].EventProcessing[eventPage.EventId]; // And here.
                                    eventProcessing.Active = 1;
                                    eventProcessing.ActionTimer = General.GetTime();
                                    eventProcessing.CurList = 0;
                                    eventProcessing.CurSlot = 0;
                                    eventProcessing.EventId = eventPage.EventId;
                                    eventProcessing.PageId = eventPage.PageId;
                                    eventProcessing.WaitingForResponse = 0;

                                    // Allocate ListLeftOff array.
                                    eventProcessing.ListLeftOff = new int[Server.Map.Instance[map].Event[eventPage.EventId].Pages[eventPage.PageId].CommandListCount];
                                    Array.Fill(eventProcessing.ListLeftOff, -1);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    General.Logger.LogError(ex, "ProcessEventCommands crashed for player {PlayerId}", i);
                }
            });

            // Process active event commands for each player.
            Parallel.ForEach(playerIds, i =>
            {
                try
                {
                    if (i < 0 || i >= Data.TempPlayer.Length) return;
                    if (!NetworkConfig.IsPlaying(i) || Data.TempPlayer[i].EventProcessingCount <= 0 || Data.TempPlayer[i].GettingMap) return;

                    int map = GetPlayerMap(i); // Cache map number
                    bool restartloop;
                    do
                    {
                        restartloop = false;
                        for (int x = 0; x <= Data.TempPlayer[i].EventProcessingCount; x++)
                        {
                            if (Data.TempPlayer[i].EventProcessing[x].Active != 1) continue;

                            ref var instance1 = ref Data.TempPlayer[i].EventProcessing[x];

                        // Basic validity checks
                        if (instance1.EventId < 0 || instance1.EventId >= Server.Map.Instance[map].Event.Length) continue;

                        bool removeEventProcess = false;

                        // Handle waiting states (shop, bank, event movement).
                        switch (instance1.WaitingForResponse)
                        {
                            case 2: // Waiting for shop to close.
                                if (Data.TempPlayer[i].InShop == -1)
                                {
                                    instance1.WaitingForResponse = 0;
                                }

                                break;
                            case 3: // Waiting for bank to close.
                                if (!Data.TempPlayer[i].InBank)
                                {
                                    instance1.WaitingForResponse = 0;
                                }

                                break;
                            case 4: // Waiting for event movement to complete.
                            {
                                //check to make sure event still exists
                                if (instance1.EventMovingId < 0 || instance1.EventMovingId >= Data.TempPlayer[i].EventMap.EventPages.Length)
                                    break;

                                if (instance1.EventMovingType == 0) // Local event.
                                {
                                    if (Data.TempPlayer[i].EventMap.EventPages[instance1.EventMovingId].MoveRouteComplete == 1)
                                    {
                                        instance1.WaitingForResponse = 0;
                                    }
                                }
                                else // Global event.
                                {
                                    //check that map still exists
                                    if (GetPlayerMap(i) < 0 || GetPlayerMap(i) >= Event.TempEventMap.Length)
                                        break;

                                    //check that event still exists.
                                    if (instance1.EventMovingId < 0 || instance1.EventMovingId >= Event.TempEventMap[GetPlayerMap(i)].Event.Length)
                                        break;

                                    if (Event.TempEventMap[GetPlayerMap(i)].Event[instance1.EventMovingId].MoveRouteComplete == 1)
                                    {
                                        instance1.WaitingForResponse = 0;
                                    }
                                }

                                break;
                            }
                        }

                        if (instance1.WaitingForResponse == 0 && instance1.ActionTimer <= General.GetTime())
                        {
                            // Process event commands until a wait, branch, or end condition is encountered.
                            bool restartlist = true;
                            bool endprocess = false;
                            while (restartlist && !endprocess && instance1.WaitingForResponse == 0)
                            {
                                restartlist = false;

                                // Check for null or out-of-bounds conditions.
                                if (instance1.ListLeftOff == null) continue; // Should not happen, but handle it.

                                var commandList = Server.Map.Instance[map].Event[instance1.EventId].Pages[instance1.PageId].CommandList;

                                // More boundary checks
                                if (instance1.CurList >= commandList.Length)
                                {
                                    removeEventProcess = true;
                                    endprocess = true;
                                    continue; // Exit the inner loop.
                                }

                                if (instance1.CurSlot >= commandList[instance1.CurList].Commands.Length)
                                {
                                    if (instance1.CurList == commandList[instance1.CurList].ParentList)
                                    {
                                        removeEventProcess = true;
                                        endprocess = true;
                                    }
                                    else
                                    {
                                        instance1.CurList = commandList[instance1.CurList].ParentList;
                                        instance1.CurSlot = 0;
                                        restartlist = true;
                                    }

                                    continue;
                                }

                                // Restore saved position in the command list, if any.
                                if (instance1.ListLeftOff[instance1.CurList] >= 0)
                                {
                                    instance1.CurSlot = instance1.ListLeftOff[instance1.CurList] + 1;
                                    instance1.ListLeftOff[instance1.CurList] = -1; // Clear the saved position.
                                }

                                // Check again, since curslot and curlist may have changed
                                if (instance1.CurList >= commandList.Length)
                                {
                                    removeEventProcess = true;
                                    endprocess = true;
                                    continue; // Exit inner loop.
                                }

                                if (instance1.CurSlot >= commandList[instance1.CurList].CommandCount)
                                {
                                    if (instance1.CurList == commandList[instance1.CurList].ParentList) //should be itself
                                    {
                                        removeEventProcess = true; // End of the main list.
                                        endprocess = true;
                                    }
                                    else
                                    {
                                        instance1.CurList = commandList[instance1.CurList].ParentList;
                                        instance1.CurSlot = 0;
                                        restartlist = true;
                                    }

                                    continue;
                                }

                                if (!restartlist && !endprocess)
                                {
                                    // Process the current event command.
                                    var command = commandList[instance1.CurList].Commands[instance1.CurSlot];

                                    switch (command.Index)
                                    {
                                        case (byte) EventCommand.AddText:
                                        {
                                            switch (command.Data2)
                                            {
                                                case 0: // Player
                                                    NetworkSend.PlayerMessage(i, command.Text1, command.Data1);
                                                    break;
                                                case 1: // Map
                                                    NetworkSend.MapMessage(map, command.Text1);
                                                    break;
                                                case 2: // Global
                                                    NetworkSend.GlobalMessage(command.Text1);
                                                    break;
                                            }

                                            break;
                                        }
                                        case (byte) EventCommand.ShowText:
                                        {
                                            var buffer = new PacketWriter();
                                            {
                                                buffer.WriteEnum(ServerPackets.SEventChat);
                                                buffer.WriteInt32(instance1.EventId);
                                                buffer.WriteInt32(instance1.PageId);
                                                buffer.WriteInt32(command.Data1); // Face Icon
                                                buffer.WriteString(ParseEventText(i, command.Text1));

                                                // Determine if there's a next command to influence display behavior.
                                                int nextCommandType = 0; // 0: None, 1: ShowText/Choices, 2: Condition
                                                if (instance1.CurSlot + 1 < commandList[instance1.CurList].CommandCount)
                                                {
                                                    byte nextIndex = (byte) commandList[instance1.CurList].Commands[instance1.CurSlot + 1].Index;
                                                    if (nextIndex == (byte) EventCommand.ShowText || nextIndex == (byte) EventCommand.ShowChoices)
                                                    {
                                                        nextCommandType = 1;
                                                    }
                                                    else if (nextIndex == (byte) EventCommand.ConditionalBranch)
                                                    {
                                                        nextCommandType = 2;
                                                    }
                                                }
                                                else //end of list
                                                    nextCommandType = 2;

                                                // Client expects: choiceCount (int), then AnotherChat (int).
                                                // For ShowText there are no choices, so choiceCount=0 and AnotherChat carries
                                                // the "next command" hint used by the UI flow.
                                                buffer.WriteInt32(0);
                                                buffer.WriteInt32(nextCommandType);
                                                PlayerService.Instance.SendDataTo(i, buffer.GetBytes());
                                            }

                                            instance1.WaitingForResponse = 1; // Wait for the client to continue (reply=0).
                                            break;
                                        }
                                        case (byte) EventCommand.ShowChoices:   
                                        {
                                            var buffer = new PacketWriter();
                                            {
                                                buffer.WriteEnum(ServerPackets.SEventChat);
                                                buffer.WriteInt32(instance1.EventId);
                                                buffer.WriteInt32(instance1.PageId);
                                                buffer.WriteInt32(command.Data5);
                                                buffer.WriteString(ParseEventText(i, command.Text1));

                                                // Determine the number of choices.
                                                int w = 0;
                                                if (!string.IsNullOrEmpty(command.Text2))
                                                {
                                                    w = 1;
                                                    if (!string.IsNullOrEmpty(command.Text3))
                                                    {
                                                        w = 2;
                                                        if (!string.IsNullOrEmpty(command.Text4))
                                                        {
                                                            w = 3;
                                                            if (!string.IsNullOrEmpty(command.Text5))
                                                            {
                                                                w = 4;
                                                            }
                                                        }
                                                    }
                                                }

                                                buffer.WriteInt32(w);

                                                // Write choice texts.
                                                for (int v = 1; v <= w; v++)
                                                {
                                                    switch (v)
                                                    {
                                                        case 1: buffer.WriteString(ParseEventText(i, command.Text2)); break;
                                                        case 2: buffer.WriteString(ParseEventText(i, command.Text3)); break;
                                                        case 3: buffer.WriteString(ParseEventText(i, command.Text4)); break;
                                                        case 4: buffer.WriteString(ParseEventText(i, command.Text5)); break;
                                                    }
                                                }

                                                // Next command logic (similar to ShowText).
                                                int nextCommandType = 0;
                                                if (instance1.CurSlot + 1 < commandList[instance1.CurList].CommandCount)
                                                {
                                                    byte nextIndex = (byte) commandList[instance1.CurList].Commands[instance1.CurSlot + 1].Index;
                                                    if (nextIndex == (byte) EventCommand.ShowText || nextIndex == (byte) EventCommand.ShowChoices)
                                                    {
                                                        nextCommandType = 1;
                                                    }
                                                    else if (nextIndex == (byte) EventCommand.ConditionalBranch)
                                                    {
                                                        nextCommandType = 2;
                                                    }
                                                }
                                                else
                                                    nextCommandType = 2;

                                                buffer.WriteInt32(nextCommandType);
                                                PlayerService.Instance.SendDataTo(i, buffer.GetBytes());
                                            }

                                            instance1.WaitingForResponse = 1; // Wait for the client reply.
                                            break;
                                        }
                                        case (byte) EventCommand.Variable:
                                        {
                                            switch (command.Data2)
                                            {
                                                case 0: // Set
                                                    Player.Instance[i].Variables[command.Data1] = command.Data3;
                                                    break;
                                                case 1: // Add
                                                    Player.Instance[i].Variables[command.Data1] += command.Data3;
                                                    break;
                                                case 2: // Subtract
                                                    Player.Instance[i].Variables[command.Data1] -= command.Data3;
                                                    break;
                                                case 3: // Random
                                                    Player.Instance[i].Variables[command.Data1] = (int) General.GetRandom.NextDouble(command.Data3, command.Data4);
                                                    break;
                                            }

                                            // Check for new event pages
                                            SpawnMapEventsFor(i, map);
                                            break;
                                        }
                                        case (byte) EventCommand.Switch:
                                        {
                                            Player.Instance[i].Switches[command.Data1] = (byte) (command.Data2 == 0 ? 0 : 1);

                                            // Check for new event pages
                                            SpawnMapEventsFor(i, map);
                                            break;
                                        }

                                        case (byte) EventCommand.SelfSwitch:
                                        {
                                            // Determine whether it's a global or local self switch.
                                            if (Server.Map.Instance[map].Event[instance1.EventId].Globals == 1)
                                            {
                                                Server.Map.Instance[map].Event[instance1.EventId].SelfSwitches[command.Data1 + 1] = (byte) (command.Data2 == 0 ? 0 : 1);
                                            }
                                            else
                                            {
                                                Data.TempPlayer[i].EventMap.EventPages[instance1.EventId].SelfSwitches[command.Data1 + 1] = (byte) (command.Data2 == 0 ? 0 : 1);
                                            }

                                            // Check for new event pages
                                            SpawnMapEventsFor(i, map);
                                            break;
                                        }
                                        case (byte) EventCommand.ConditionalBranch:
                                        {
                                            bool conditionMet = false;
                                            var branch = command.ConditionalBranch;

                                            switch (branch.Condition)
                                            {
                                                case 0: // Variable
                                                {
                                                    int playerVar = Player.Instance[i].Variables[branch.Data1];
                                                    switch (branch.Data2)
                                                    {
                                                        case 0: conditionMet = playerVar == branch.Data3; break;
                                                        case 1: conditionMet = playerVar >= branch.Data3; break;
                                                        case 2: conditionMet = playerVar <= branch.Data3; break;
                                                        case 3: conditionMet = playerVar > branch.Data3; break;
                                                        case 4: conditionMet = playerVar < branch.Data3; break;
                                                        case 5: conditionMet = playerVar != branch.Data3; break;
                                                    }

                                                    break;
                                                }
                                                case 1: // Switch
                                                {
                                                    bool switchState = Player.Instance[i].Switches[branch.Data1] == 1;
                                                    conditionMet = (branch.Data2 == 0 && switchState) || (branch.Data2 == 1 && !switchState);
                                                    break;
                                                }
                                                case 2: // Item
                                                    conditionMet = Player.HasItem(i, branch.Data1) >= branch.Data2;
                                                    break;
                                                case 3: // Class
                                                    conditionMet = Player.Instance[i].Job == branch.Data1;
                                                    break;
                                                case 4: // Skill
                                                    conditionMet = HasSkill(i, branch.Data1);
                                                    break;
                                                case 5: // Level
                                                {
                                                    int level = GetPlayerLevel(i);
                                                    switch (branch.Data2)
                                                    {
                                                        case 0: conditionMet = level == branch.Data1; break;
                                                        case 1: conditionMet = level >= branch.Data1; break;
                                                        case 2: conditionMet = level <= branch.Data1; break;
                                                        case 3: conditionMet = level > branch.Data1; break;
                                                        case 4: conditionMet = level < branch.Data1; break;
                                                        case 5: conditionMet = level != branch.Data1; break;
                                                    }

                                                    break;
                                                }
                                                case 6: // Self Switch
                                                {
                                                    bool selfSwitchState;
                                                    if (Server.Map.Instance[map].Event[instance1.EventId].Globals == 1)
                                                        selfSwitchState = Server.Map.Instance[map].Event[instance1.EventId].SelfSwitches[branch.Data1 + 1] == 1;
                                                    else
                                                        selfSwitchState = Data.TempPlayer[i].EventMap.EventPages[instance1.EventId].SelfSwitches[branch.Data1 + 1] == 1;

                                                    conditionMet = (branch.Data2 == 0 && selfSwitchState) || (branch.Data2 == 1 && !selfSwitchState);
                                                    break;
                                                }

                                                case 7: //Timer - Not currently implemented
                                                    break;
                                                case 8: // Gender
                                                    conditionMet = Player.Instance[i].Sex == branch.Data1;
                                                    break;
                                                case 9: // Time of Day
                                                    conditionMet = Clock.Instance.TimeOfDay == (TimeOfDay) branch.Data1;
                                                    break;
                                            }

                                            // Set the next command list and slot based on the condition.
                                            instance1.ListLeftOff[instance1.CurList] = instance1.CurSlot;
                                            instance1.CurList = conditionMet ? branch.CommandList : branch.ElseCommandList;
                                            instance1.CurSlot = 0;
                                            endprocess = true; //end process so we dont increment curslot, but instead start at the top of the conditional list.

                                            break;
                                        }

                                        case (byte) EventCommand.ExitEventProcess:
                                            removeEventProcess = true;
                                            endprocess = true;
                                            break;

                                        case (byte) EventCommand.ChangeItems:
                                        {
                                            switch (command.Data2)
                                            {
                                                case 0: // Set
                                                    if (Player.HasItem(i, command.Data1) > 0)
                                                    {
                                                        SetInvValue(i, Player.FindItemSlot(i, command.Data1), command.Data3);
                                                    }

                                                    break;
                                                case 1: // Give
                                                    Player.GiveInv(i, command.Data1, command.Data3, 0, true);
                                                    break;
                                                case 2: // Take
                                                {
                                                    int itemAmount = Player.HasItem(i, command.Data1);
                                                    if (itemAmount >= command.Data3)
                                                    {
                                                        Player.TakeInv(i, command.Data1, command.Data3);
                                                    }

                                                    break;
                                                }
                                            }

                                            NetworkSend.Inventory(i);
                                            break;
                                        }

                                        case (byte) EventCommand.RestoreHealth:
                                            SetPlayerVital(i, Core.Globals.Vital.Health, Script.Instance?.GetPlayerMaxVital(i, Core.Globals.Vital.Health));
                                            NetworkSend.Vital(i, Core.Globals.Vital.Health);
                                            break;

                                        case (byte) EventCommand.RestoreMana:
                                            SetPlayerVital(i, Core.Globals.Vital.Mana, Script.Instance?.GetPlayerMaxVital(i, Core.Globals.Vital.Mana));
                                            NetworkSend.Vital(i, Core.Globals.Vital.Mana);
                                            break;

                                        case (byte) EventCommand.RestoreStamina:
                                            SetPlayerVital(i, Core.Globals.Vital.Stamina, Script.Instance?.GetPlayerMaxVital(i, Core.Globals.Vital.Stamina));
                                            NetworkSend.Vital(i, Core.Globals.Vital.Stamina);
                                            break;

                                        case (byte) EventCommand.GiveExperience:
                                            SetPlayerExperience(i, GetPlayerExperience(i) + command.Data1);
                                            Player.OnLevel(i);
                                            break;

                                        case (byte) EventCommand.LevelUp:
                                            SetPlayerExperience(i, Script.Instance?.GetPlayerNextLevel(i));
                                            Player.OnLevel(i);
                                            break;

                                        case (byte) EventCommand.ChangePoints:
                                            SetPlayerPoints(i, GetPlayerPoints(i) + command.Data1);
                                            NetworkSend.PlayerData(i);
                                            break;

                                        case (byte) EventCommand.ChangeLevel:
                                            SetPlayerLevel(i, GetPlayerLevel(i) + command.Data1);
                                            SetPlayerExperience(i, 0);
                                            NetworkSend.PlayerData(i);
                                            NetworkSend.Experience(i);
                                            break;

                                        case (byte) EventCommand.ChangeSkills:
                                        {
                                            if (command.Data2 == 0) // Learn
                                            {
                                                if (FindOpenSkill(i) >= 0 && !HasSkill(i, command.Data1))
                                                {
                                                    SetSkill(i, FindOpenSkill(i), command.Data1);
                                                }
                                            }
                                            else if (command.Data2 == 1) // Forget
                                            {
                                                for (int p = 0; p < Core.Globals.Variables.MaxPlayerSkills; p++)
                                                {
                                                    if (Player.Instance[i].Skill[p].Num == command.Data1)
                                                    {
                                                        SetSkill(i, p, 0);
                                                    }
                                                }
                                            }

                                            NetworkSend.PlayerSkills(i);
                                            break;
                                        }

                                        case (byte) EventCommand.ChangeJob:
                                            Player.Instance[i].Job = (byte) command.Data1;
                                            NetworkSend.PlayerData(i);
                                            break;

                                        case (byte) EventCommand.ChangeSprite:
                                            SetPlayerSprite(i, command.Data1);
                                            NetworkSend.PlayerData(i);
                                            break;

                                        case (byte) EventCommand.ChangeSex:
                                            Player.Instance[i].Sex = (byte) (command.Data1 == 0 ? Sex.Male : Sex.Female);
                                            NetworkSend.PlayerData(i);
                                            break;

                                        case (byte) EventCommand.SetPlayerKillable:
                                            Player.Instance[i].Pk = (command.Data1 == 0 ? false : true);
                                            NetworkSend.PlayerData(i);
                                            break;

                                        case (byte) EventCommand.WarpPlayer:
                                        {
                                            int dir = command.Data4 == 0 ? Player.Instance[i].Dir : (byte) (command.Data4 - 1);
                                            Player.OnWarp(i, command.Data1, command.Data2, command.Data3, dir);
                                            break;
                                        }

                                        case (byte) EventCommand.SetMoveRoute:
                                        {
                                            // Check if the event exists.
                                            if (command.Data1 < Server.Map.Instance[map].Event.Length)
                                            {
                                                if (Server.Map.Instance[map].Event[command.Data1].Globals == 1) // Global event
                                                {
                                                    // Directly modify the global event.
                                                    ref var globalEvent = ref Event.TempEventMap[map].Event[command.Data1];
                                                    globalEvent.MoveType = 2; // Custom route
                                                    globalEvent.IgnoreIfCannotMove = command.Data2;
                                                    globalEvent.RepeatMoveRoute = command.Data3;
                                                    globalEvent.MoveRouteCount = command.MoveRouteCount;
                                                    if (command.MoveRouteCount > 0)
                                                    {
                                                        globalEvent.MoveRoute = new MoveRoute[command.MoveRouteCount];
                                                        Array.Copy(command.MoveRoute, globalEvent.MoveRoute, command.MoveRouteCount);
                                                    }

                                                    globalEvent.MoveRouteStep = 0;
                                                    globalEvent.MoveRouteComplete = (command.MoveRouteCount == 0) ? 1 : 0; //if routecount is 0, complete = true
                                                }
                                                else // Local event
                                                {
                                                    // Modify the local event copy for this player.
                                                    ref var localEvent = ref Data.TempPlayer[i].EventMap.EventPages[command.Data1]; // Assuming Data1 is the event index
                                                    localEvent.MoveType = 2;
                                                    localEvent.IgnoreIfCannotMove = command.Data2;
                                                    localEvent.RepeatMoveRoute = command.Data3;
                                                    localEvent.MoveRouteCount = command.MoveRouteCount;
                                                    if (command.MoveRouteCount > 0)
                                                    {
                                                        localEvent.MoveRoute = new MoveRoute[command.MoveRouteCount];
                                                        Array.Copy(command.MoveRoute, localEvent.MoveRoute, command.MoveRouteCount);
                                                    }

                                                    localEvent.MoveRouteStep = 0;
                                                    localEvent.MoveRouteComplete = (command.MoveRouteCount == 0) ? 1 : 0; // If no route, it's complete.
                                                }
                                            }

                                            break;
                                        }

                                        case (byte) EventCommand.PlayAnimation:
                                        {
                                            switch (command.Data2)
                                            {
                                                case 0: // On Player
                                                    NetworkSend.PlayAnimation(map, command.Data1, GetPlayerX(i), GetPlayerY(i), (byte) TargetType.Player, i);
                                                    break;
                                                case 1: // On Event
                                                {
                                                    //check for valid event
                                                    if (command.Data3 < 0 || command.Data3 >= Server.Map.Instance[map].Event.Length)
                                                        break;

                                                    if (Server.Map.Instance[map].Event[command.Data3].Globals == 1)
                                                    {
                                                        // Play on global event.
                                                        NetworkSend.PlayAnimation(map, command.Data1,
                                                            Server.Map.Instance[map].Event[command.Data3].X,
                                                            Server.Map.Instance[map].Event[command.Data3].Y);
                                                    }
                                                    else
                                                    {
                                                        //check that local event exists for this player.
                                                        if (command.Data3 < 0 || command.Data3 >= Data.TempPlayer[i].EventMap.EventPages.Length)
                                                            break;

                                                        // Play on local event.
                                                        NetworkSend.PlayAnimation(map, command.Data1,
                                                            Data.TempPlayer[i].EventMap.EventPages[command.Data3].X,
                                                            Data.TempPlayer[i].EventMap.EventPages[command.Data3].Y,
                                                            (byte) TargetType.Event, command.Data3);
                                                    }

                                                    break;
                                                }
                                                case 2: // On Coordinates
                                                    NetworkSend.PlayAnimation(map, command.Data1, command.Data3, command.Data4, 0, 0);
                                                    break;
                                            }

                                            break;
                                        }

                                        case (byte) EventCommand.PlayBgm:
                                        {
                                            var buffer = new PacketWriter();
                                            {
                                                buffer.WriteEnum(ServerPackets.SPlayBgm);
                                                buffer.WriteString(command.Text1);
                                                PlayerService.Instance.SendDataTo(i, buffer.GetBytes());
                                            }

                                            break;
                                        }
                                        case (byte) EventCommand.FadeOutBgm:
                                        {
                                            var buffer = new PacketWriter(4);
                                            buffer.WriteEnum(ServerPackets.SFadeoutBgm);
                                            PlayerService.Instance.SendDataTo(i, buffer.GetBytes());
                                            break;
                                        }

                                        case (byte) EventCommand.PlaySound:
                                        {
                                            var buffer = new PacketWriter();
                                            {
                                                buffer.WriteEnum(ServerPackets.SPlaySound);
                                                buffer.WriteString(command.Text1);
                                                buffer.WriteInt32(Server.Map.Instance[map].Event[instance1.EventId].X);
                                                buffer.WriteInt32(Server.Map.Instance[map].Event[instance1.EventId].Y);
                                                PlayerService.Instance.SendDataTo(i, buffer.GetBytes());
                                            }

                                            break;
                                        }

                                        case (byte) EventCommand.StopSound:
                                        {
                                            var buffer = new PacketWriter(4);
                                            buffer.WriteEnum(ServerPackets.SStopSound);
                                            PlayerService.Instance.SendDataTo(i, buffer.GetBytes());
                                            break;
                                        }
                                        case (byte) EventCommand.SetAccessLevel:
                                            SetPlayerAccess(i, (byte) command.Data1);
                                            NetworkSend.PlayerData(i);
                                            break;

                                        case (byte) EventCommand.OpenShop:
                                        {
                                            // Check if the shop exists and has a valid name.
                                            if (command.Data1 > 0 && command.Data1 < Core.Globals.Variables.MaxShops && command.Data1 < Shop.Instance.Count && !string.IsNullOrEmpty(Shop.Instance[command.Data1].Name))
                                            {
                                                NetworkSend.OpenShop(i, command.Data1);
                                                Data.TempPlayer[i].InShop = command.Data1;
                                                instance1.WaitingForResponse = 2; // Wait for shop to close.
                                            }

                                            break;
                                        }
                                        case (byte) EventCommand.OpenBank:
                                            NetworkSend.Bank(i);
                                            Data.TempPlayer[i].InBank = true;
                                            instance1.WaitingForResponse = 3; // Wait for bank to close.
                                            break;

                                        case (byte) EventCommand.ShowChatBubble:
                                        {
                                            ColorName color = ColorName.Blue; // Or any default color you prefer
                                            switch (command.Data1)
                                            {
                                                case (byte) TargetType.Player:
                                                    NetworkSend.ChatBubble(map, i, command.Data1, command.Text1, (int) color);
                                                    break;
                                                case (byte) TargetType.Npc:
                                                    NetworkSend.ChatBubble(map, command.Data2, command.Data1, command.Text1, (int) color);
                                                    break;
                                                case (byte) TargetType.Event:
                                                    NetworkSend.ChatBubble(map, command.Data2, command.Data1, command.Text1, (int) color);
                                                    break;
                                            }

                                            break;
                                        }

                                        case (byte) EventCommand.Label:
                                            // No action needed, just a label for GoToLabel.
                                            break;

                                        case (byte) EventCommand.GoToLabel:
                                            // Find the label and update the command list position.
                                            FindEventLabel(command.Text1, map, instance1.EventId, instance1.PageId, ref instance1.CurSlot, ref instance1.CurList, ref instance1.ListLeftOff);
                                            break;

                                        case (byte) EventCommand.SpawnNpc:
                                            if (command.Data1 > 0 && command.Data1 < Server.Map.Instance[map].Npc.Length) // Check if Npc exists
                                            {
                                                MapNpc.OnSpawn(command.Data1, map);
                                            }

                                            break;

                                        case (byte) EventCommand.FadeIn:
                                            NetworkSend.SpecialEffect(i, Event.EffectTypeFadeIn);
                                            break;

                                        case (byte) EventCommand.FadeOut:
                                            NetworkSend.SpecialEffect(i, Event.EffectTypeFadeOut);
                                            break;

                                        case (byte) EventCommand.FlashScreen:
                                            NetworkSend.SpecialEffect(i, Event.EffectTypeFlash);
                                            break;

                                        case (byte) EventCommand.SetFog:
                                            NetworkSend.SpecialEffect(i, Event.EffectTypeFog, command.Data1, command.Data2, command.Data3);
                                            break;

                                        case (byte) EventCommand.SetWeather:
                                            NetworkSend.SpecialEffect(i, Event.EffectTypeWeather, command.Data1, command.Data2);
                                            break;

                                        case (byte) EventCommand.SetScreenTint:
                                            NetworkSend.SpecialEffect(i, Event.EffectTypeTint, command.Data1, command.Data2, command.Data3, command.Data4);
                                            break;

                                        case (byte) EventCommand.Wait:
                                            instance1.ActionTimer = General.GetTime() + command.Data1;
                                            break;

                                        case (byte) EventCommand.ShowPicture:
                                        {
                                            var buffer = new PacketWriter();
                                            {
                                                buffer.WriteEnum(ServerPackets.SPic);
                                                buffer.WriteInt32(instance1.EventId); // Event ID.
                                                buffer.WriteByte((byte) command.Data1); // Picture ID.
                                                buffer.WriteByte((byte) command.Data2); // X
                                                buffer.WriteByte((byte) command.Data3); // Y
                                                buffer.WriteByte((byte) command.Data4); // Transparency
                                                PlayerService.Instance.SendDataTo(i, buffer.GetBytes());
                                            }

                                            break;
                                        }
                                        case (byte) EventCommand.HidePicture:
                                        {
                                            var buffer = new PacketWriter(8);
                                            {
                                                buffer.WriteEnum(ServerPackets.SPic);
                                                buffer.WriteByte(0); // Hide picture.
                                                PlayerService.Instance.SendDataTo(i, buffer.GetBytes());
                                            }

                                            break;
                                        }
                                        case (byte) EventCommand.WaitMovementCompletion:
                                        {
                                            // Ensure the event exists.
                                            if (command.Data1 < Server.Map.Instance[map].Event.Length)
                                            {
                                                if (Server.Map.Instance[map].Event[command.Data1].Globals == 1)
                                                {
                                                    instance1.WaitingForResponse = 4;
                                                    instance1.EventMovingId = command.Data1; // Global event ID.
                                                    instance1.EventMovingType = 1; // Global.
                                                }
                                                else
                                                {
                                                    //check that local event exists on player
                                                    if (command.Data1 < 0 || command.Data1 >= Data.TempPlayer[i].EventMap.EventPages.Length)
                                                        break;

                                                    instance1.WaitingForResponse = 4;
                                                    instance1.EventMovingId = command.Data1; // Local event ID.
                                                    instance1.EventMovingType = 0; // Local.
                                                }
                                            }

                                            break;
                                        }
                                        case (byte) EventCommand.HoldPlayer:
                                        {
                                            var buffer = new PacketWriter(8);
                                            buffer.WriteEnum(ServerPackets.SHoldPlayer);
                                            buffer.WriteInt32(0); // Hold
                                            PlayerService.Instance.SendDataTo(i, buffer.GetBytes());
                                            break;
                                        }
                                        case (byte) EventCommand.ReleasePlayer:
                                        {
                                            var buffer = new PacketWriter(8);
                                            {
                                                buffer.WriteInt32((int) ServerPackets.SHoldPlayer);
                                                buffer.WriteInt32(1); // Release
                                                PlayerService.Instance.SendDataTo(i, buffer.GetBytes());
                                            }

                                            break;
                                        }
                                    }
                                }

                                // Increment to the next command, unless we've branched or ended.
                                if (!endprocess)
                                    instance1.CurSlot++;
                            }
                        }


                        // Clean up finished event processes.
                        if (removeEventProcess)
                        {
                            instance1.Active = 0;
                            restartloop = true;
                        }
                        }
                    } while (restartloop);
                }
                catch (Exception ex)
                {
                    General.Logger.LogError(ex, "ProcessEventCommands (active) crashed for player {PlayerId}", i);
                }
            });
        }

        public static void UpdateEventLogic()
        {
            // These functions have been optimized to reduce redundant calls and improve clarity.
            RemoveDeadEvents();
            SpawnNewEvents();
            OnMove();
            ProcessLocalMovement();
            ProcessEventCommands();
        }


        public static string ParseEventText(int index, string txt)
        {
            if (string.IsNullOrEmpty(txt))
                return string.Empty;

            // PlayerBase.Instance is a List; the player id may be invalid during disconnect/races.
            if (index < 0 || Player.Instance == null || index >= Player.Instance.Count)
                return txt;

            var player = Player.Instance[index];
            if (player == null)
                return txt;

            var playerName = player.Name ?? string.Empty;

            string playerClass = string.Empty;
            var jobId = (int)player.Job;
            if (jobId >= 0 && Job.Instance != null && jobId < Job.Instance.Count)
            {
                playerClass = Job.Instance[jobId].Name ?? string.Empty;
            }

            // Use StringBuilder for efficient string manipulation.
            var sb = new System.Text.StringBuilder(txt);

            sb.Replace("/name", playerName);
            sb.Replace("/p", playerName);
            sb.Replace("$playername$", playerName);
            sb.Replace("$playerclass$", playerClass);

            // Process variables (/v[variableIndex]).
            int start = sb.ToString().IndexOf("/v"); // Find the first occurrence.
            while (start >= 0)
            {
                int end = start + 2;
                // Find the end of the number.
                while (end < sb.Length && char.IsDigit(sb[end]))
                {
                    end++;
                }

                if (end > start + 2) // Ensure we found a number.
                {
                    string varIndexStr = sb.ToString(start + 2, end - (start + 2));
                    if (int.TryParse(varIndexStr, out int varIndex))
                    {
                        // Make sure the variable index is within bounds
                        if (player.Variables != null && varIndex >= 0 && varIndex < player.Variables.Length)
                        {
                            sb.Remove(start, end - start);
                            sb.Insert(start, player.Variables[varIndex].ToString());
                        }
                        else
                        {
                            //invalid variable, remove it from the output.
                            sb.Remove(start, end - start);
                        }
                    }
                    else //should never occur, but just in case.
                        sb.Remove(start, end - start); //if it wasn't a valid number, remove it.
                }
                else // If no number, remove /v
                {
                    sb.Remove(start, 2);
                }

                start = sb.ToString().IndexOf("/v"); //check for any others
            }

            return sb.ToString();
        }

        public static void FindEventLabel(string label, int map, int eventId, int pageId, ref int curSlot, ref int curList, ref int[] listLeftOff)
        {
            // Check for valid map, event, and page.
            if (map < 0 || map >= Server.Map.Instance.Count || eventId < 0 || eventId >= Server.Map.Instance[map].Event.Length ||
                pageId < 0 || pageId >= Server.Map.Instance[map].Event[eventId].Pages.Length)
            {
                //invalid event, don't do anything.
                return;
            }

            int tmpCurSlot = curSlot;
            int tmpCurList = curList;
            int[] tmpListLeftOff = listLeftOff;

            // Initialize data structures.
            var commandList = Server.Map.Instance[map].Event[eventId].Pages[pageId].CommandList;

            // Check if commandList is null
            if (commandList == null)
                return;

            listLeftOff = new int[commandList.Length];
            Array.Fill(listLeftOff, -1);
            int[] currentListOption = new int[commandList.Length];

            curList = 0;
            curSlot = 0;

            bool removeEventProcess = false;
            bool restartlist;

            while (!removeEventProcess)
            {
                restartlist = false;

                // Restore position if returning from a nested list.
                if (listLeftOff[curList] >= 0)
                {
                    curSlot = listLeftOff[curList];
                    listLeftOff[curList] = -1;
                }

                // Check for out-of-bounds conditions.
                if (curList >= commandList.Length)
                {
                    removeEventProcess = true; // Invalid list index.
                    continue;
                }

                if (curSlot >= commandList[curList].CommandCount)
                {
                    if (curList == commandList[curList].ParentList) //should be itself
                    {
                        removeEventProcess = true; // Reached the end of a top-level list.
                    }
                    else
                    {
                        curList = commandList[curList].ParentList;
                        curSlot = 0;
                        restartlist = true;
                    }

                    continue;
                }

                if (!restartlist && !removeEventProcess)
                {
                    // Get the current command.
                    var command = commandList[curList].Commands[curSlot];

                    switch (command.Index)
                    {
                        case (byte) EventCommand.ShowChoices:
                        {
                            int w = 0;
                            if (!string.IsNullOrEmpty(command.Text2))
                            {
                                w = 1;
                                if (!string.IsNullOrEmpty(command.Text3))
                                {
                                    w = 2;
                                    if (!string.IsNullOrEmpty(command.Text4))
                                    {
                                        w = 3;
                                        if (!string.IsNullOrEmpty(command.Text5))
                                        {
                                            w = 4;
                                        }
                                    }
                                }
                            }

                            if (w > 0)
                            {
                                if (currentListOption[curList] < w)
                                {
                                    currentListOption[curList]++;
                                    listLeftOff[curList] = curSlot; // Save current position.

                                    // Jump to the appropriate choice's command list.
                                    switch (currentListOption[curList])
                                    {
                                        case 1: curList = command.Data1; break;
                                        case 2: curList = command.Data2; break;
                                        case 3: curList = command.Data3; break;
                                        case 4: curList = command.Data4; break;
                                    }

                                    curSlot = 0; // Start at the beginning of the new list.
                                }
                                else
                                {
                                    currentListOption[curList] = 0; // Reset for next time.
                                }
                            }

                            break;
                        }
                        case (byte) EventCommand.ConditionalBranch:
                        {
                            // Handle conditional branches (simplified logic).
                            if (currentListOption[curList] == 0)
                            {
                                // First visit: Execute the "if" branch.
                                listLeftOff[curList] = curSlot;
                                curList = command.ConditionalBranch.CommandList;
                                curSlot = 0;
                            }
                            else if (currentListOption[curList] == 1)
                            {
                                // Second visit: Execute the "else" branch (if it exists).
                                listLeftOff[curList] = curSlot;
                                curList = command.ConditionalBranch.ElseCommandList;
                                curSlot = 0;
                            }

                            //else currentlistoption = 2, so continue on.
                            currentListOption[curList] = (currentListOption[curList] + 1) % 3; //prepare for next visit

                            break;
                        }
                        case (byte) EventCommand.Label:
                        {
                            // Check if this is the target label.
                            if (command.Text1 == label)
                            {
                                return; // Found the label, return to the caller.
                            }

                            break;
                        }
                    }

                    curSlot++; // Move to the next command.
                }
            }

            // Label not found, restore original values.
            curList = tmpCurList;
            curSlot = tmpCurSlot;
            listLeftOff = tmpListLeftOff;
        }

        // Replace FindNpcPath with an A* pathfinding implementation
        public static int FindNpcPath(int map, double npc, int targetx, int targety)
        {
            // Validate map and NPC
            if (map < 0 || map >= Server.Map.Instance.Count || npc < 0 || npc >= Core.Globals.Variables.MaxMapNpcs)
                return 4;

            int startX = MapNpc.Instance[map, (int)npc].X;
            int startY = MapNpc.Instance[map, (int)npc].Y;
            int goalX = targetx < 0 ? 0 : targetx;
            int goalY = targety < 0 ? 0 : targety;

            int maxX = Server.Map.Instance[map].MaxX;
            int maxY = Server.Map.Instance[map].MaxY;

            // Early out if already at target
            if (startX == goalX && startY == goalY)
                return 4;

            // Node structure for A*
            var openSet = new PriorityQueue<(int x, int y), int>();
            var cameFrom = new Dictionary<(int, int), (int, int)>();
            var gScore = new Dictionary<(int, int), int>();
            var fScore = new Dictionary<(int, int), int>();

            (int x, int y) start = (startX, startY);
            (int x, int y) goal = (goalX, goalY);

            gScore[start] = 0;
            fScore[start] = Heuristic(start, goal);
            openSet.Enqueue(start, fScore[start]);

            // Directions: Right, Down, Up, Left (to match original return values)
            int[] dx = {1, 0, 0, -1};
            int[] dy = {0, 1, -1, 0};
            int[] dirResult = {(int) Direction.Right, (int) Direction.Down, (int) Direction.Up, (int) Direction.Left};

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();

                if (current.Equals(goal))
                {
                    // Reconstruct path to get the first step
                    var path = new List<(int x, int y)>();
                    var node = current;
                    while (cameFrom.ContainsKey(node))
                    {
                        path.Add(node);
                        node = cameFrom[node];
                    }

                    path.Reverse();
                    if (path.Count == 0)
                        return 4;
                    var firstStep = path[0];
                    for (int d = 0; d < 4; d++)
                    {
                        if (startX + dx[d] == firstStep.x && startY + dy[d] == firstStep.y)
                            return dirResult[d];
                    }

                    return 4;
                }

                for (int d = 0; d < 4; d++)
                {
                    int nx = current.x + dx[d];
                    int ny = current.y + dy[d];
                    if (nx < 0 || ny < 0 || nx > maxX || ny > maxY)
                        continue;
                        
                    if (!IsTileWalkable(map, nx, ny))
                        continue;

                    var neighbor = (nx, ny);
                    int tentativeG = gScore[current] + 1;
                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);
                        if (!openSet.UnorderedItems.Any(item => item.Element.Equals(neighbor)))
                            openSet.Enqueue(neighbor, fScore[neighbor]);
                    }
                }
            }

            return 4; // No path found

            // Manhattan distance
            static int Heuristic((int x, int y) a, (int x, int y) b) => Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);

            // Use Event's IsTileWalkable if available, otherwise treat 0 as walkable
            static bool IsTileWalkable(int map, int x, int y)
            {
                // Use Event.IsTileWalkable if available, otherwise always return true for this stub
                // Replace with actual walkability logic as needed
                return true;
            }
        }

        public static async System.Threading.Tasks.Task SpawnAllMapGlobalEvents()
        {
            // Use Task.Run to avoid blocking the main thread.
            await System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < Core.Globals.Variables.MaxMaps; i++)
                {
                    SpawnGlobalEvents(i).ConfigureAwait(false);
                }
            });
        }

        public static async System.Threading.Tasks.Task SpawnGlobalEvents(int map)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                // Check if the map exists and has events.
                if (map < 0 || map >= Server.Map.Instance.Count || Server.Map.Instance[map].EventCount <= 0)
                {
                    return;
                }

                // Initialize the temporary event map.
                Event.TempEventMap[map].EventCount = 0;
                Array.Resize(ref Event.TempEventMap[map].Event, 1); // Start with size 1, resize as needed.

                for (int i = 0; i < Server.Map.Instance[map].EventCount; i++)
                {
                    // Check for valid global events.
                    if (Server.Map.Instance[map].Event[i].PageCount > 0 && Server.Map.Instance[map].Event[i].Globals == 1)
                    {
                        // Add a new event to the temporary map.
                        Event.TempEventMap[map].EventCount++;
                        Array.Resize(ref Event.TempEventMap[map].Event, Event.TempEventMap[map].EventCount + 1); // +1 for easier indexing
                        ref var tempEvent = ref Event.TempEventMap[map].Event[Event.TempEventMap[map].EventCount];

                        // Set initial event properties.
                        tempEvent.X = Server.Map.Instance[map].Event[i].X;
                        tempEvent.Y = Server.Map.Instance[map].Event[i].Y;
                        tempEvent.Dir = Server.Map.Instance[map].Event[i].Pages[0].GraphicType == 1
                            ? (byte)((Server.Map.Instance[map].Event[i].Pages[0].GraphicY % 4) switch
                            {
                                0 => Direction.Down,
                                1 => Direction.Left,
                                2 => Direction.Right,
                                _ => Direction.Up
                            })
                            : (byte)Direction.Down;
                        tempEvent.Active = 0;
                        tempEvent.MoveType = Server.Map.Instance[map].Event[i].Pages[0].MoveType;

                        if (tempEvent.MoveType == 2) // Custom Move Route
                        {
                            int moveRouteCount = Server.Map.Instance[map].Event[i].Pages[0].MoveRouteCount;
                            tempEvent.MoveRouteCount = moveRouteCount;

                            if (moveRouteCount > 0)
                            {
                                // Copy the move route.
                                tempEvent.MoveRoute = new MoveRoute[moveRouteCount];
                                Array.Copy(Server.Map.Instance[map].Event[i].Pages[0].MoveRoute, tempEvent.MoveRoute, moveRouteCount);
                                tempEvent.MoveRouteComplete = 0; // Reset completion status.
                            }
                            else
                            {
                                tempEvent.MoveRouteComplete = 1;
                            }
                        }
                        else
                        {
                            tempEvent.MoveRouteComplete = 1; // Not a move route, so considered complete.
                        }

                        tempEvent.RepeatMoveRoute = Server.Map.Instance[map].Event[i].Pages[0].RepeatMoveRoute;
                        tempEvent.IgnoreIfCannotMove = Server.Map.Instance[map].Event[i].Pages[0].IgnoreMoveRoute;
                        tempEvent.MoveFreq = Server.Map.Instance[map].Event[i].Pages[0].MoveFreq;
                        tempEvent.MoveSpeed = Server.Map.Instance[map].Event[i].Pages[0].MoveSpeed;
                        tempEvent.WalkThrough = Server.Map.Instance[map].Event[i].Pages[0].WalkThrough;
                        tempEvent.FixedDir = Server.Map.Instance[map].Event[i].Pages[0].DirFix;
                        tempEvent.WalkingAnim = Server.Map.Instance[map].Event[i].Pages[0].IdleAnim;
                        tempEvent.ShowName = Server.Map.Instance[map].Event[i].Pages[0].ShowName;
                    }
                }
            });
        }

        public static void SpawnMapEventsFor(int index, int map)
        {
            // Check for valid map.
            if (map < 0 || map >= Server.Map.Instance.Count)
            {
                return;
            }

            // Reset player's event data.
            Data.TempPlayer[index].EventMap.CurrentEvents = 0;
            Array.Resize(ref Data.TempPlayer[index].EventMap.EventPages, 1);

            // Initialize event processing array.
            if (Server.Map.Instance[map].EventCount > 0)
            {
                Array.Resize(ref Data.TempPlayer[index].EventProcessing, Server.Map.Instance[map].EventCount + 1); //+1 for easier indexing
                Data.TempPlayer[index].EventProcessingCount = Server.Map.Instance[map].EventCount;
            }
            else
            {
                Array.Resize(ref Data.TempPlayer[index].EventProcessing, 1); //+1 for easier indexing
                Data.TempPlayer[index].EventProcessingCount = 0;
            }

            if (Server.Map.Instance[map].EventCount <= 0) return;

            // Iterate through map events.
            for (int i = 0; i < Server.Map.Instance[map].EventCount; i++)
            {
                int p = -1;

                // Check if event and its pages exist
                if (Server.Map.Instance[map].Event[i].Pages == null) continue;
                if (Server.Map.Instance[map].Event[i].PageCount <= 0) continue;

                // Find the highest-priority page that meets conditions.
                for (int z = 0; z < Server.Map.Instance[map].Event[i].PageCount; z++)
                {
                    bool spawnCurrentEvent = true;
                    ref var page = ref Server.Map.Instance[map].Event[i].Pages[z]; // Use ref for direct modification.
                    bool variableConditionMet = false;

                    // Check conditions (Variable, Switch, Item, Self Switch).
                    if (page.ChkVariable == 1)
                    {
                        int playerVar = Player.Instance[index].Variables[page.VariableIndex];
                        switch (page.VariableCompare)
                        {
                            case 0: variableConditionMet = playerVar == page.VariableCondition; break;
                            case 1: variableConditionMet = playerVar > page.VariableCondition; break;
                            case 2: variableConditionMet = playerVar < page.VariableCondition; break;
                            case 3: variableConditionMet = playerVar != page.VariableCondition; break;
                            case 4: variableConditionMet = playerVar >= page.VariableCondition; break;
                            case 5: variableConditionMet = playerVar <= page.VariableCondition; break;
                        }

                        if (!variableConditionMet)
                            spawnCurrentEvent = false;
                    }

                    if (page.ChkSwitch == 1)
                    {
                        // Using XOR for switch check, handles both expecting true and false efficiently
                        if (!((page.SwitchCompare == 1) ^ (Player.Instance[index].Switches[page.SwitchIndex] == 0))) //we want true
                            spawnCurrentEvent = false;
                    }

                    if (page.ChkHasItem == 1 && Player.HasItem(index, page.HasItemIndex) == 0)
                    {
                        spawnCurrentEvent = false;
                    }

                    if (page.ChkSelfSwitch == 1)
                    {
                        int compare = page.SelfSwitchCompare; // 0 or 1, no need to check both values explicitly.
                        bool selfSwitchState;

                        if (Server.Map.Instance[map].Event[i].Globals == 1)
                            selfSwitchState = Server.Map.Instance[map].Event[i].SelfSwitches[page.SelfSwitchIndex] == compare;
                        else
                            selfSwitchState = false; // Local self switches are not checked when spawning.

                        if (!selfSwitchState)
                            spawnCurrentEvent = false;
                    }

                    if (spawnCurrentEvent)
                    {
                        p = z; // Store the valid page index.
                    }
                }

                // Spawn the event if a valid page was found.
                if (p >= 0)
                {
                    int z = p;

                    Data.TempPlayer[index].EventMap.CurrentEvents++;
                    Array.Resize(ref Data.TempPlayer[index].EventMap.EventPages, Data.TempPlayer[index].EventMap.CurrentEvents + 1);
                    ref var instance1 = ref Data.TempPlayer[index].EventMap.EventPages[Data.TempPlayer[index].EventMap.CurrentEvents];

                    ref var eventPage = ref Server.Map.Instance[map].Event[i].Pages[z];

                    // Set up the event page data.
                    instance1.Dir = eventPage.GraphicType == 1
                        ? (byte)((eventPage.GraphicY % 4) switch
                        {
                            0 => Direction.Down,
                            1 => Direction.Left,
                            2 => Direction.Right,
                            _ => Direction.Up
                        })
                        : (byte)0;

                    instance1.Graphic = eventPage.Graphic;
                    instance1.GraphicType = eventPage.GraphicType;
                    instance1.GraphicX = eventPage.GraphicX;
                    instance1.GraphicY = eventPage.GraphicY;
                    instance1.GraphicX2 = eventPage.GraphicX2;
                    instance1.GraphicY2 = eventPage.GraphicY2;
                    instance1.MovementSpeed = eventPage.MoveSpeed switch
                    {
                        0 => 2,
                        1 => 3,
                        2 => 4,
                        3 => 6,
                        4 => 12,
                        5 => 24,
                        _ => DefaultMovementSpeed
                    };

                    if (Server.Map.Instance[map].Event[i].Globals == 1)
                    {
                        // Use global event's position and direction.
                        instance1.X = Event.TempEventMap[map].Event[i].X * 32;
                        instance1.Y = Event.TempEventMap[map].Event[i].Y * 32;
                        instance1.Dir = Event.TempEventMap[map].Event[i].Dir;
                        instance1.MoveRouteStep = Event.TempEventMap[map].Event[i].MoveRouteStep;
                    }
                    else
                    {

                        instance1.X = Server.Map.Instance[map].Event[i].X * 32;
                        instance1.Y = Server.Map.Instance[map].Event[i].Y * 32;

                        instance1.MoveRouteStep = 0;
                    }

                    instance1.Position = eventPage.Position;
                    instance1.EventId = i; // Map event ID.
                    instance1.PageId = z;
                    instance1.Visible = true; // Always visible when initially spawned.
                    instance1.MoveType = eventPage.MoveType;

                    if (instance1.MoveType == 2) // Custom move route
                    {
                        instance1.MoveRouteCount = eventPage.MoveRouteCount;

                        if (eventPage.MoveRouteCount > 0)
                        {
                            instance1.MoveRoute = new MoveRoute[eventPage.MoveRouteCount];
                            Array.Copy(eventPage.MoveRoute, instance1.MoveRoute, eventPage.MoveRouteCount);
                            instance1.MoveRouteComplete = 0; // Reset completion status
                        }
                        else
                            instance1.MoveRouteComplete = 1;
                    }
                    else
                    {
                        instance1.MoveRouteComplete = 1;
                    }

                    instance1.RepeatMoveRoute = eventPage.RepeatMoveRoute;
                    instance1.IgnoreIfCannotMove = eventPage.IgnoreMoveRoute;
                    instance1.MoveFreq = eventPage.MoveFreq;
                    instance1.MoveSpeed = eventPage.MoveSpeed;
                    instance1.WalkingAnim = eventPage.IdleAnim;
                    instance1.WalkThrough = eventPage.WalkThrough;
                    instance1.ShowName = eventPage.ShowName;
                    instance1.FixedDir = eventPage.DirFix;
                }
            }

            // Send spawn event packets to the player.
            var buffer = new PacketWriter();
            buffer.WriteEnum(ServerPackets.SSpawnEvent);
            int total = Data.TempPlayer[index].EventMap.CurrentEvents;
            buffer.WriteInt32(total);

            // EventPages is 1-based in this code-path; client expects 0..(count-1) ids.
            for (int slot = 1; slot <= total; slot++)
            {
                ref var eventPage = ref Data.TempPlayer[index].EventMap.EventPages[slot];

                // Write a sequential id for client-side array indexing.
                buffer.WriteInt32(slot - 1);

                buffer.WriteString(Server.Map.Instance[map].Event[eventPage.EventId].Name);
                buffer.WriteByte(eventPage.Dir);
                buffer.WriteByte(eventPage.GraphicType);
                buffer.WriteInt32(eventPage.Graphic);
                buffer.WriteInt32(eventPage.GraphicX);
                buffer.WriteInt32(eventPage.GraphicX2);
                buffer.WriteInt32(eventPage.GraphicY);
                buffer.WriteInt32(eventPage.GraphicY2);
                buffer.WriteInt32(eventPage.MovementSpeed);
                buffer.WriteInt32(eventPage.X);
                buffer.WriteInt32(eventPage.Y);
                buffer.WriteByte(eventPage.Position);
                buffer.WriteBoolean(eventPage.Visible);
                buffer.WriteByte(Map.Instance[map].Event[eventPage.EventId].Pages[eventPage.PageId].IdleAnim);
                buffer.WriteByte(Map.Instance[map].Event[eventPage.EventId].Pages[eventPage.PageId].DirFix);
                buffer.WriteInt32(Map.Instance[map].Event[eventPage.EventId].Pages[eventPage.PageId].WalkThrough);
                buffer.WriteInt32(Map.Instance[map].Event[eventPage.EventId].Pages[eventPage.PageId].ShowName);
            }

            PlayerService.Instance.SendDataTo(index, buffer.GetBytes());
        }

        public static bool TriggerEvent(int player, int eventId, byte triggerType, int targetX, int targetY)
        {
            // 1. Validate player and map
            if (player < 0)
                return false;

            int map = GetPlayerMap(player);
            if (map < 0 || map >= Server.Map.Instance.Count)
                return false;

            // 2. Find the relevant event for the player
            var eventMap = Data.TempPlayer[player].EventMap;
            int localEvent = -1;
            for (int slot = 1; slot <= eventMap.CurrentEvents; slot++)
            {
                if (eventMap.EventPages[slot].EventId == eventId)
                {
                    localEvent = slot;
                    break;
                }
            }

            if (localEvent == -1)
                return false; // Event not found

            ref var eventPage = ref eventMap.EventPages[localEvent];
            var mapEvent = Server.Map.Instance[map].Event[eventPage.EventId];
            var page = mapEvent.Pages[eventPage.PageId];

            // 3. Check trigger type
            if (page.Trigger != triggerType)
                return false;

            // 4. Determine the target tile based on trigger type.
            // Action Button (0): trigger the tile in front of the player.
            // Player Touch (1): trigger the tile the player is on.
            var playerX = GetPlayerX(player);
            var playerY = GetPlayerY(player);
            if (triggerType == 0)
            {
                (int x, int y)? offset = GetOffsetByDirection(GetPlayerDir(player), playerX, playerY, Server.Map.Instance[map]);
                if (offset == null)
                    return false;

                (targetX, targetY) = offset.Value;
            }
            else
            {
                targetX = playerX;
                targetY = playerY;
            }

            // 5. Validate the target tile matches the event tile.
            // Event page X/Y are in pixels and may be mid-step (not aligned to tile origin).
            // Consider the event "on" the target tile if its tile-sized bounds intersect the target tile.
            var tileLeft = targetX * Constants.TileSize;
            var tileTop = targetY * Constants.TileSize;
            var tileRight = tileLeft + Constants.TileSize;
            var tileBottom = tileTop + Constants.TileSize;

            var evLeft = eventPage.X;
            var evTop = eventPage.Y;
            var evRight = evLeft + Constants.TileSize;
            var evBottom = evTop + Constants.TileSize;

            if (!(evLeft < tileRight && evRight > tileLeft && evTop < tileBottom && evBottom > tileTop))
                return false;

            // 6. Begin event processing if applicable
            if (page.CommandListCount > 0)
            {
                ref var eventProcessing = ref Data.TempPlayer[player].EventProcessing[eventPage.EventId];

                eventProcessing.Active = 1;
                eventProcessing.ActionTimer = General.GetTime();
                eventProcessing.CurList = 0;
                eventProcessing.CurSlot = 0;
                eventProcessing.EventId = eventPage.EventId;
                eventProcessing.PageId = eventPage.PageId;
                eventProcessing.WaitingForResponse = 0;
                eventProcessing.ListLeftOff = new int[page.CommandListCount];
                Array.Fill(eventProcessing.ListLeftOff, -1);
                // Event successfully triggered and processing started.
                return true;
            
            }

            return false;
        }

        // Helper to calculate tile offsets based on player direction and map bounds
        private static (int, int)? GetOffsetByDirection(byte direction, int x, int y, Core.Objects.MapBase map)
        {
            int newX = x, newY = y;
            switch ((Direction) direction)
            {
                case Direction.Up:
                    if (y > 0) newY = y - 1;
                    else return null;
                    break;
                case Direction.Down:
                    if (y < map.MaxY) newY = y + 1;
                    else return null;
                    break;
                case Direction.Left:
                    if (x > 0) newX = x - 1;
                    else return null;
                    break;
                case Direction.Right:
                    if (x < map.MaxX) newX = x + 1;
                    else return null;
                    break;
                case Direction.UpRight:
                    if (x < map.MaxX && y > 0)
                    {
                        newX = x + 1;
                        newY = y - 1;
                    }
                    else return null;

                    break;
                case Direction.UpLeft:
                    if (x > 0 && y > 0)
                    {
                        newX = x - 1;
                        newY = y - 1;
                    }
                    else return null;

                    break;
                case Direction.DownLeft:
                    if (x > 0 && y < map.MaxY)
                    {
                        newX = x - 1;
                        newY = y + 1;
                    }
                    else return null;

                    break;
                case Direction.DownRight:
                    if (x < map.MaxX && y < map.MaxY)
                    {
                        newX = x + 1;
                        newY = y + 1;
                    }
                    else return null;

                    break;
                default:
                    return null;
            }

            return (newX, newY);
        }
    }
}