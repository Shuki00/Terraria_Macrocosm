using Macrocosm.Common.Drawing;
using Macrocosm.Common.Drawing.Particles;
using Macrocosm.Common.Netcode;
using Macrocosm.Common.Players;
using Macrocosm.Common.Sets;
using Macrocosm.Common.Storage;
using Macrocosm.Common.Subworlds;
using Macrocosm.Common.Systems.UI;
using Macrocosm.Common.Utils;
using Macrocosm.Common.WorldGeneration;
using Macrocosm.Content.Items.LiquidContainers;
using Macrocosm.Content.Items.Tech;
using Macrocosm.Content.Particles;
using Macrocosm.Content.Rockets.Customization;
using Macrocosm.Content.Rockets.LaunchPads;
using Macrocosm.Content.Rockets.Modules;
using Macrocosm.Content.Sounds;
using Macrocosm.Content.WorldGeneration.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Macrocosm.Content.Rockets
{
    public partial class Rocket : IInventoryOwner
    {
        public enum ActionState
        {
            Idle,
            PreLaunch,
            StaticFire,
            Flight,
            Landing,
            Docking,
            Undocking
        }

        /// <summary> The rocket's identifier </summary>
        public int WhoAmI = -1;

        /// <summary> Whether the rocket is currently active </summary>
        [NetSync] public bool Active;

        /// <summary> The rocket's top-left coodrinates in the world </summary>
        [NetSync] public Vector2 Position;

        /// <summary> The rocket's velocity </summary>
        [NetSync] public Vector2 Velocity;

        /// <summary> The rocket's sequence timer </summary>
        [NetSync] public int FlightTime;

        [NetSync] public ActionState State = ActionState.Idle;

        /// <summary> The initial vertical position </summary>
        [NetSync] public float StartPositionY;

        /// <summary> The target world to fly towards </summary>
        [NetSync] public string TargetWorld = "";

        /// <summary> The target landing position </summary>
        [NetSync] public Vector2 TargetTravelPosition;
        private LaunchPad targetLaunchPad;

        /// <summary> The amount of fuel currently stored in the rocket, as an absolute value </summary>
        [NetSync] public float Fuel = 0f;

        /// <summary> The rocket's fuel capacity as an absolute value </summary>
        [NetSync] public float FuelCapacity = DefaultFuelCapacity;

        /// <summary> The default fuel capacity </summary>
        public const float DefaultFuelCapacity = 1000f;

        /// <summary> The rocket's current world, "Earth" if active and not in a subworld. Other mod's subworlds have the mod name prepended </summary>
        [NetSync] public string CurrentWorld = "";

        /// <summary> Whether travelling to or from orbit </summary>
        [NetSync] public bool OrbitTravel;

        /// <summary> Whether the rocket is active in the current world and should be updated and visible </summary>
        public bool ActiveInCurrentWorld => Active && CurrentWorld == MacrocosmSubworld.CurrentID;

        /// <summary> The world Y coordinate for entering the target subworld </summary>
        private float FlightExitPosition => 60 * 16f;
        private float UndockingExitPosition => Main.maxTilesY * 16f - 60 * 16f;


        /// <summary> The size of the rocket's bounds </summary>
        public Vector2 Size => CalculateSize();

        /// <summary> The rocket's bounds width </summary>
        public int Width => (int)Size.X;

        /// <summary> The rocket's bounds height </summary>
        public int Height => (int)Size.Y;

        /// <summary> The rectangle occupied by the Rocket in the world </summary>
        public Rectangle Bounds => new((int)Position.X, (int)Position.Y, Width, Height);

        /// <summary> The rocket's center in world coordinates </summary>
        public Vector2 Center
        {
            get => Position + Size / 2f;
            set => Position = value - Size / 2f;
        }

        /// <summary> The layer this rocket is drawn in </summary>
        public RocketDrawLayer DrawLayer = RocketDrawLayer.BeforeNPCs;

        /// <summary> The rocket transparency. Currently it only works when the rocket is drawn using a RenderTarget </summary>
        public float Transparency { get; set; } = 1f;

        /// <summary> This rocket's nameplate </summary>
        public Nameplate Nameplate { get; set; } = new();

        /// <summary> The Rocket's name, if not set by the user, defaults to a localized "Rocket" name </summary>
        public string DisplayName
            => Nameplate.IsValid() ? Nameplate.Text : Language.GetTextValue("Mods.Macrocosm.Common.Rocket");

        /// <summary> List of all the modules a rocket can have </summary>
        public List<RocketModule> AvailableModules { get; } = new();

        /// <summary> List of all the modules currently active on this rocket </summary>
        public List<RocketModule> ActiveModules => AvailableModules.Where(module => module.Active).ToList();

        /// <summary> The rocket's active modules ordered by their <see cref="RocketModule.DrawPriority"/> </summary>
        public List<RocketModule> ActiveModulesByDrawPriority => ActiveModules.OrderBy(module => module.DrawPriority).ToList();
        public List<string> ActiveModuleNames => ActiveModules.Select((m) => m.Name).ToList();

        public int PreLaunchDuration = 160;

        public int StaticFireDuration = 60;
        /// <summary> The progress of the <see cref="StaticFire"/> </summary>
        public float StaticFireProgress => Utility.InverseLerp(0, StaticFireDuration, FlightTime, clamped: true);

        /// <summary> The flight sequence progress </summary>
        public float FlightProgress { get; set; }
        private float EasedFlightProgress => Utility.QuartEaseIn(FlightProgress);

        /// <summary> The landing sequence progress </summary>
        public float LandingProgress { get; set; }
        private float EasedLandingProgress => Utility.QuadraticEaseOut(LandingProgress);

        /// <summary> The docking sequence progress </summary>
        public float DockingProgress { get; set; }
        private float EasedDockingProgress => Utility.QuadraticEaseOut(DockingProgress);

        /// <summary> The landing sequence progress </summary>
        public float UndockingProgress { get; set; }
        private float EasedUndockingProgress => Utility.QuadraticEaseIn(UndockingProgress);

        private bool ranFirstUpdate;
        private bool canAnimate = true;

        private bool forcedStationaryAppearance;
        /// <summary> Whether this rocket is forced in a stationary (i.e. landed) state, visually </summary>
        public bool ForcedStationaryAppearance
        {
            get => forcedStationaryAppearance;
            set
            {
                forcedStationaryAppearance = value;

                if (value)
                    forcedFlightAppearance = false;

                ResetAnimation();
            }
        }

        private bool forcedFlightAppearance;
        /// <summary> Whether this rocket is forced in a full flight state, visually </summary>
        public bool ForcedFlightAppearance
        {
            get => forcedFlightAppearance;
            set
            {
                forcedFlightAppearance = value;

                if (value)
                    forcedStationaryAppearance = false;

                ResetAnimation();
            }
        }

        public Inventory Inventory { get; set; }

        public const int DefaultGeneralInventorySize = 50;

        public const int SpecialInventorySlot_CustomizationUnlock = 0;
        public const int SpecialInventorySlot_FuelTank = 1;
        public const int SpecialInventorySlot_Count = 2;

        public const int DefaultTotalInventorySize = DefaultGeneralInventorySize + SpecialInventorySlot_Count;

        public Vector2 InventoryPosition => Center;
        public int InventorySerializationIndex => WhoAmI;

        /// <summary> Instatiates a rocket. Use <see cref="Create(Vector2)"/> for spawning in world and proper syncing. </summary>
        public Rocket(List<string> activeModules = null)
        {
            foreach (string moduleName in ModuleNames)
            {
                var module = CreateModule(moduleName);
                module.Active = (activeModules ?? DefaultModuleNames).Contains(moduleName);
                module.SetRocket(this);
                AvailableModules.Add(module);
            }

            Inventory = new Inventory(DefaultTotalInventorySize, this);
        }

        public static readonly List<string> DefaultModuleNames = ["CommandPod", "ServiceModule", "ReactorModule", "EngineModule", "BoosterLeft", "BoosterRight"];

        public static readonly List<string> ModuleNames = ["CommandPod", "PayloadPod", "ServiceModule", "UnmannedTug", "ReactorModule", "EngineModule", "BoosterLeft", "BoosterRight"];

        private RocketModule CreateModule(string moduleName)
        {
            return moduleName switch
            {
                "CommandPod" => new CommandPod(),
                "PayloadPod" => new PayloadPod(),
                "ServiceModule" => new ServiceModule(),
                "UnmannedTug" => new UnmannedTug(),
                "ReactorModule" => new ReactorModule(),
                "EngineModule" => new EngineModule(),
                "BoosterLeft" => new BoosterLeft(),
                "BoosterRight" => new BoosterRight(),
                _ => throw new ArgumentException($"Unknown module name: {moduleName}")
            };
        }

        private void OnFirstUpdate()
        {
            ResetRenderTarget();
            ResetAnimation();
            ReserveSlots();
        }

        /// <summary> Update the rocket </summary>
        public void Update()
        {
            if (!ranFirstUpdate)
            {
                OnFirstUpdate();
                ranFirstUpdate = true;
            }

            CurrentWorld = MacrocosmSubworld.CurrentID;

            UpdateModules();

            Velocity = GetCollisionVelocity();
            Position += Velocity;

            Effects();
            PlaySound();

            float gravity = MacrocosmSubworld.GetGravityMultiplier();
            float gravityFactor = 0.7f + 0.3f * gravity;

            switch (State)
            {
                case ActionState.Idle:

                    Interact();
                    LookForCommander();
                    Velocity.Y += 0.1f * gravity;

                    break;

                case ActionState.PreLaunch:

                    FlightTime++;
                    Velocity.Y += 0.1f * gravity;

                    if (FlightTime >= PreLaunchDuration && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        FlightTime = 0;
                        State = ActionState.StaticFire;
                        SyncCommonData();
                    }

                    break;

                case ActionState.StaticFire:

                    FlightTime++;
                    Velocity.Y += 0.1f * gravity;

                    if (FlightTime >= StaticFireDuration)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            FlightTime = 0;
                            State = ActionState.Flight;
                            SyncCommonData();
                        }

                        ResetAnimation();
                        SoundEngine.PlaySound(SFX.RocketLaunch with { Volume = 1f, PlayOnlyIfFocused = true });
                    }

                    break;

                case ActionState.Flight:

                    FlightTime++;

                    float launchDistance = Math.Abs(StartPositionY - FlightExitPosition);
                    float launchDuration = 10f * 60f * gravityFactor;
                    float launchIncrement = launchDistance / launchDuration;

                    Position = new Vector2(Position.X, MathHelper.Lerp(StartPositionY, FlightExitPosition, EasedFlightProgress));
                    FlightProgress += launchIncrement / launchDistance;
                    FlightProgress = MathHelper.Clamp(FlightProgress, 0f, 1f);

                    UpdateModuleAnimation();
                    if (EasedFlightProgress > 0.01f)
                        StartModuleAnimation(landing: false);

                    if (GetRocketPlayer(Main.myPlayer).InRocket)
                        Main.BlackFadeIn = (int)(255f * EasedFlightProgress);

                    if (Position.Y < FlightExitPosition + 1)
                    {
                        FlightProgress = 0f;
                        LandingProgress = 0f;
                        DockingProgress = 0f;
                        UndockingProgress = 0f;

                        ResetAnimation();

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            FlightTime = 0;
                            Velocity = Vector2.Zero;
                            State = OrbitTravel ? ActionState.Docking : ActionState.Landing;
                            HandleTravel();

                            SyncEverything();
                        }
                    }

                    break;

                case ActionState.Landing:

                    if (TargetTravelPosition == default && LandingProgress < float.Epsilon && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        TargetTravelPosition = GetLandingSite(Utility.SpawnWorldPosition);
                        SyncCommonData();
                    }

                    float landingDistance = Math.Abs(FlightExitPosition - TargetTravelPosition.Y + Height);
                    float landingDuration = 10f * 60f * (1f / gravityFactor);
                    float landingIncrement = landingDistance / landingDuration;
                    Position = new Vector2(TargetTravelPosition.X - Width / 2 - 8, MathHelper.Lerp(FlightExitPosition, TargetTravelPosition.Y - Height, EasedLandingProgress));
                    LandingProgress += landingIncrement / landingDistance;
                    LandingProgress = MathHelper.Clamp(LandingProgress, 0f, 1f);

                    UpdateModuleAnimation();

                    if (EasedLandingProgress > 0.95f)
                        StartModuleAnimation(landing: true);

                    if (GetRocketPlayer(Main.myPlayer).InRocket)
                        Main.BlackFadeIn = (int)(255f * (1f - EasedLandingProgress));

                    if (LandingProgress >= 1f - float.Epsilon)
                    {
                        State = ActionState.Idle;
                        ResetAnimation();
                    }

                    break;

                case ActionState.Docking:

                    if (TargetTravelPosition == default && DockingProgress < float.Epsilon)
                    {
                        Structure module = new BaseSpaceStationModule();
                        TargetTravelPosition = GetLandingSite(Utility.SpawnWorldPosition) + new Vector2(0, Height + (int)(module.Size.Y * 16f));
                    }

                    float dockingDistance = Math.Abs(UndockingExitPosition - TargetTravelPosition.Y);
                    float dockingDuration = 10f * 60f * (1f / gravityFactor);
                    float dockingIncrement = dockingDistance / dockingDuration;

                    Position = new Vector2(TargetTravelPosition.X - Width / 2 - 8, MathHelper.Lerp(UndockingExitPosition, TargetTravelPosition.Y - Height, EasedDockingProgress));
                    DockingProgress += dockingIncrement / dockingDistance;
                    DockingProgress = MathHelper.Clamp(DockingProgress, 0f, 1f);

                    if (GetRocketPlayer(Main.myPlayer).InRocket)
                        Main.BlackFadeIn = (int)(255f * (1f - EasedDockingProgress));

                    if (DockingProgress >= 1f - float.Epsilon)
                    {
                        State = ActionState.Idle;
                    }

                    break;

                case ActionState.Undocking:
                    float undockingDistance = Math.Abs(StartPositionY - UndockingExitPosition);
                    float undockingDuration = 5f * 60f * (1f / gravityFactor);
                    float undockingIncrement = undockingDistance / undockingDuration;

                    Position = new Vector2(Position.X, MathHelper.Lerp(StartPositionY, UndockingExitPosition, EasedUndockingProgress));
                    UndockingProgress += undockingIncrement / undockingDistance;
                    UndockingProgress = MathHelper.Clamp(UndockingProgress, 0f, 1f);

                    if (GetRocketPlayer(Main.myPlayer).InRocket)
                        Main.BlackFadeIn = (int)(255f * EasedUndockingProgress);

                    if (Position.Y > UndockingExitPosition - 1)
                    {
                        FlightProgress = 0f;
                        LandingProgress = 0f;
                        DockingProgress = 0f;
                        UndockingProgress = 0f;
                        ResetAnimation();

                        if(Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            FlightTime = 0;
                            Velocity = Vector2.Zero;
                            State = OrbitTravel ? ActionState.Docking : ActionState.Landing;
                            Position.Y = OrbitTravel ? UndockingExitPosition : FlightExitPosition;
                            HandleTravel();
                        }
                    }

                    break;

            }

            if (Position.X < 10 * 16 || Position.X > (Main.maxTilesX - 10) * 16 || Position.Y < 10 * 16 || Position.Y > (Main.maxTilesY - 10) * 16)
                State = ActionState.Idle;
            SyncCommonData();


            if (Transparency < 1f)
                Transparency += 0.01f;

            if (Transparency > 1f)
                Transparency = 1f;

            //Main.NewText(State);
        }

        /// <summary> Safely despawn the rocket </summary>
        public void Despawn()
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                if (CheckPlayerInRocket(Main.myPlayer))
                {
                    GetRocketPlayer(Main.myPlayer).InRocket = false;
                    GetRocketPlayer(Main.myPlayer).IsCommander = false;
                }
            }
            else
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (CheckPlayerInRocket(i))
                    {
                        GetRocketPlayer(i).InRocket = false;
                        GetRocketPlayer(i).IsCommander = false;
                    }
                }
            }

            bool sync = Main.netMode != NetmodeID.SinglePlayer;
            bool fromClient = Main.netMode == NetmodeID.MultiplayerClient;
            Inventory.DropAllItems(Center, sync, fromClient);

            Active = false;

            if (sync)
                SyncCommonData();
        }

        /// <summary> Gets the RocketPlayer bound to the provided player ID </summary>
        /// <param name="playerID"> The player whoAmI </param>
        public RocketPlayer GetRocketPlayer(int playerID) => Main.player[playerID].GetModPlayer<RocketPlayer>();

        /// <summary> Gets the commander of this rocket </summary>
        public RocketPlayer GetCommander()
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                if (GetRocketPlayer(Main.myPlayer).InRocket)
                    return GetRocketPlayer(Main.myPlayer);
                else
                    return null;
            }
            else
            {
                if (TryFindingCommander(out int id))
                    return GetRocketPlayer(id);
                else if (AnyEmbarkedPlayers(out id))
                    return GetRocketPlayer(id);
                else
                    return null;
            }
        }

        /// <summary> Checks whether the provided player ID is on this rocket </summary>
        /// <param name="playerID"> The player whoAmI </param>
        public bool CheckPlayerInRocket(int playerID) => Main.player[playerID].active && GetRocketPlayer(playerID).InRocket && GetRocketPlayer(playerID).RocketID == WhoAmI;

        /// <summary> Checks whether the provided player ID is a commander on this rocket </summary>
        /// <param name="playerID"> The player whoAmI </param>
        public bool CheckPlayerCommander(int playerID) => Main.player[playerID].active && GetRocketPlayer(playerID).IsCommander && GetRocketPlayer(playerID).RocketID == WhoAmI;

        /// <summary> Checks whether this rocket has a commander </summary>
        /// <param name="playerID"> The commander player whoAmI </param>
        public bool TryFindingCommander(out int playerID)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (!Main.player[i].active)
                    continue;

                RocketPlayer rocketPlayer = GetRocketPlayer(i);
                if (CheckPlayerInRocket(i) && rocketPlayer.IsCommander)
                {
                    playerID = i;
                    return true;
                }
            }

            playerID = -1;
            return false;
        }

        /// <summary> Check whether there are currently any players inside this rocket </summary>
        /// <param name="first"> The ID of the first player found in rocket, will be -1 if none found </param>
        public bool AnyEmbarkedPlayers(out int first)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                first = Main.myPlayer;
                return CheckPlayerInRocket(first);
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (!Main.player[i].active)
                    continue;

                if (CheckPlayerInRocket(i))
                {
                    first = i;
                    return true;
                }
            }
            first = -1;
            return false;
        }

        /// <summary> 
        /// Launches the rocket, with syncing. 
        /// For <paramref name="targetWorld"/>, use the ID format respective to <see cref="MacrocosmSubworld.CurrentID"/> (mod name prepended) 
        /// </summary>
        public void Launch(string targetWorld, LaunchPad targetLaunchPad = null)
        {
            State = TargetIsParentSubworld(targetWorld) ? ActionState.Undocking : ActionState.PreLaunch;

            Main.playerInventory = false;

            StartPositionY = Position.Y;
            TargetWorld = targetWorld;

            OrbitTravel = false;

            this.targetLaunchPad = targetLaunchPad;

            Fuel -= GetFuelCost(targetWorld);

            SyncCommonData();
        }

        public void Launch(OrbitSubworld targetOrbitSubworld)
        {
            Launch(targetOrbitSubworld.ID);
            OrbitTravel = true;
            SyncCommonData();
        }

        public bool TargetIsParentSubworld(string targetWorld) => OrbitSubworld.IsOrbitSubworld(CurrentWorld) && OrbitSubworld.GetParentID(CurrentWorld) == targetWorld;

        public float GetFuelCost(string targetWorld) => RocketFuelLookup.GetFuelCost(MacrocosmSubworld.CurrentID, targetWorld);

        /// <summary> Checks whether the flight path is obstructed by solid blocks </summary>
        public bool CheckObstruction(bool downwards)
        {
            if (!downwards)
            {
                int startY = (int)(Position.Y / 16);
                for (int offsetX = 0; offsetX < (Width / 16); offsetX++)
                {
                    if (Utility.GetFirstTileCeiling((int)(Position.X / 16f) + offsetX, startY, solid: true) > 10)
                        return false;
                }
            }
            else
            {
                int startY = (int)((Position.Y + Height) / 16);
                for (int offsetX = 0; offsetX < (Width / 16); offsetX++)
                {
                    if (Utility.GetFirstTileFloor((int)(Position.X / 16f) + offsetX, startY, solid: true) < Main.maxTilesY - 10)
                        return false;
                }
            }


            return true;
        }

        // TODO: make this account for the Rocket's width
        public Vector2 GetLandingSite(Vector2 initialTarget)
        {
            Point tileCoords = initialTarget.ToTileCoordinates();
            int surfaceY = Utility.GetFirstTileFloor(tileCoords.X, 10, solid: true);

            if (surfaceY != Main.maxTilesY - 10)
                return new Vector2(initialTarget.X, surfaceY * 16f);
            else
                return initialTarget;
        }

        public bool AtPosition(Vector2 position) => Vector2.Distance(Center, position) < Width;

        public bool AtCurrentLaunchpad(LaunchPad launchPad, string worldId)
        {
            if (launchPad == null)
            {
                // Check for position at world spawn disabled for now
                /*
                if (worldId == MacrocosmSubworld.CurrentID)
                    return AtPosition(Utility.SpawnWorldPosition);
                else
                */
                return false;
            }

            if (worldId == MacrocosmSubworld.CurrentID)
                return launchPad.RocketID == WhoAmI;
            else
                return false;
        }


        // Gets the position of a module with respect to the provided origin
        private Vector2 GetModuleRelativePosition(RocketModule module, Vector2 origin) => origin + module.Offset;

        private void UpdateModules()
        {
            foreach (RocketModule module in ActiveModules)
                module.Position = GetModuleRelativePosition(module, Position);
        }

        private Vector2 CalculateSize()
        {
            if (ActiveModules == null || ActiveModules.Count == 0)
                return new(0, 0);

            float maxX = float.MinValue;
            float maxY = float.MinValue;
            foreach (var module in ActiveModules)
            {
                Vector2 modulePosition = GetModuleRelativePosition(module, Vector2.Zero);
                Vector2 moduleSize = new Vector2(module.Width, module.Height);
                float moduleRight = modulePosition.X + moduleSize.X;
                float moduleBottom = modulePosition.Y + moduleSize.Y;
                maxX = Math.Max(maxX, moduleRight);
                maxY = Math.Max(maxY, moduleBottom);
            }

            float calculatedWidth = maxX;
            float calculatedHeight = maxY;

            return new(calculatedWidth, calculatedHeight);
        }

        public bool CheckTileCollision()
        {
            foreach (RocketModule module in ActiveModules)
                if (Math.Abs(Collision.TileCollision(module.Position, Velocity, module.Width, module.Height).Y) > 0.1f)
                    return true;

            return false;
        }

        // Interaction logic
        private void Interact()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            if (MouseCanInteract() && Bounds.InPlayerInteractionRange(TileReachCheckSettings.Simple) && State == ActionState.Idle)
            {
                RocketPlayer rocketPlayer = GetRocketPlayer(Main.myPlayer);

                if (Main.mouseRight && Main.mouseRightRelease)
                {
                    if (!rocketPlayer.InRocket)
                    {
                        bool noCommanderInRocket = (Main.netMode == NetmodeID.SinglePlayer) || !TryFindingCommander(out _);
                        rocketPlayer.EmbarkPlayerInRocket(WhoAmI, noCommanderInRocket);
                    }
                    else if (!UISystem.Active)
                    {
                        Utility.UICloseOthers();
                        UISystem.ShowRocketUI(this);
                    }
                }
                else if (!UISystem.Active)
                {
                    if (rocketPlayer.InRocket)
                    {
                        Main.LocalPlayer.cursorItemIconEnabled = true;
                        Main.LocalPlayer.cursorItemIconID = ModContent.ItemType<Computer>();
                    }
                    else
                    {
                        Main.LocalPlayer.noThrow = 2;
                        CursorIcon.Current = CursorIcon.Rocket;
                    }
                }
            }
        }

        public void ResetAnimation()
        {
            canAnimate = true;

            foreach (RocketModule module in ActiveModules)
            {
                if (module is AnimatedRocketModule animatedModule)
                {
                    if (forcedStationaryAppearance)
                    {
                        animatedModule.CurrentFrame = animatedModule.NumberOfFrames - 1;
                    }
                    else if (forcedFlightAppearance)
                    {
                        animatedModule.CurrentFrame = 0;
                    }
                    else
                    {
                        if (State is ActionState.Landing or ActionState.Docking or ActionState.Undocking)
                            animatedModule.CurrentFrame = 0;
                        else
                            animatedModule.CurrentFrame = animatedModule.NumberOfFrames - 1;
                    }
                }
            }
        }


        private Vector2 GetCollisionVelocity()
        {
            return Collision.TileCollision(Position, Velocity, Width, Height - 2);
        }

        private bool MouseCanInteract()
        {
            if (LaunchPadManager.GetLaunchPadAtTileCoordinates(MacrocosmSubworld.CurrentID, Main.MouseWorld.ToTileCoordinates16()) != null)
                return false;

            foreach (RocketModule module in ActiveModules.Where((module) => module.Interactible))
                if (module.Bounds.Contains(Main.MouseWorld.ToPoint()))
                    return true;

            return false;
        }

        // Updates the commander player in real time, in multiplayer scenarios
        private void LookForCommander()
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            if (AnyEmbarkedPlayers(out int id) && !TryFindingCommander(out _))
            {
                GetRocketPlayer(id).IsCommander = true;
                NetMessage.SendData(MessageID.SyncPlayer, number: id);
            }
        }

        private void UpdateModuleAnimation()
        {
            bool animationActive = false;
            foreach (RocketModule module in AvailableModules)
            {
                if (module is AnimatedRocketModule animatedModule)
                {
                    animatedModule.UpdateAnimation();
                    animationActive |= animatedModule.IsAnimationActive;
                }
            }

            if (animationActive)
                ResetRenderTarget();
        }

        private void StartModuleAnimation(bool landing = false)
        {
            if (canAnimate)
            {
                foreach (RocketModule module in AvailableModules)
                {
                    if (module is AnimatedRocketModule animatedModule)
                    {
                        if (landing)
                            animatedModule.StartAnimation();
                        else
                            animatedModule.StartReverseAnimation();
                    }
                }

                SoundEngine.PlaySound(SFX.RocketLandingLeg with
                {
                    Volume = 1f,
                    PlayOnlyIfFocused = true,
                    MaxInstances = 1,
                    SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
                },
                Center, updateCallback: (sound) =>
                {
                    sound.Position = Center;
                    return ActiveInCurrentWorld;
                });
            }

            canAnimate = false;
        }

        private void ReserveSlots()
        {
            Inventory.SetReserved(
                SpecialInventorySlot_CustomizationUnlock,
                CheckUnlockableItemUnlocked
            );

            Inventory.SetReserved(
                SpecialInventorySlot_FuelTank,
                (item) => ItemSets.LiquidContainerData[item.type].Valid && ItemSets.LiquidContainerData[item.type].LiquidType == Liquids.LiquidType.RocketFuel,
                Lang.GetItemName(ModContent.ItemType<Canister>()),
                ModContent.Request<Texture2D>(ContentSamples.ItemsByType[ModContent.ItemType<Canister>()].ModItem.Texture + "_Blueprint")
            );
        }


        private void Effects()
        {
            if (State == ActionState.Idle)
                return;

            float gravityFactor = 0.7f + 0.3f * MacrocosmSubworld.GetGravityMultiplier();
            float atmoDesityFactor = 0.5f + 0.5f * MacrocosmSubworld.GetAtmosphericDensity(Center);

            Point tilePos = (Position + new Vector2(Width / 2f, Height)).ToTileCoordinates();
            Point closestTile = Utility.GetClosestTile(tilePos.X, tilePos.Y, -1, 15, (t) => Main.tileSolid[t.TileType] && !t.IsActuated);
            closestTile.Y += 1;

            float lightIntensity = 0f;
            float screenshakeIntensity = 0f;

            switch (State)
            {
                case ActionState.StaticFire:
                    {
                        lightIntensity = StaticFireProgress;
                        screenshakeIntensity = 5f * Utility.QuadraticEaseOut(StaticFireProgress);

                        int count = MacrocosmSubworld.GetAtmosphericDensity(Center) < 1f ? 1 : (int)(4f * atmoDesityFactor * StaticFireProgress);
                        SpawnSmokeExhaustTrail(countPerTick: count);

                        if (Main.rand.NextFloat() < StaticFireProgress)
                            SpawnTileDust(closestTile, 1);
                        break;
                    }

                case ActionState.Flight:
                    {
                        lightIntensity = 10f;
                        screenshakeIntensity = FlightProgress < 0.05f ? 30f : 15f * (1f - Utility.QuadraticEaseOut(FlightProgress));

                        int count = MacrocosmSubworld.GetAtmosphericDensity(Center) < 1f ? 2 : (int)(5f * atmoDesityFactor);
                        SpawnSmokeExhaustTrail(countPerTick: count);

                        if (EasedFlightProgress < 0.1f)
                            SpawnTileDust(closestTile, 2);
                        break;
                    }

                case ActionState.Landing:
                    {
                        lightIntensity = 10f;
                        screenshakeIntensity = 2f * Utility.QuadraticEaseOut(LandingProgress);

                        int count = MacrocosmSubworld.GetAtmosphericDensity(Center) < 1f ? 1 : (int)(3f * atmoDesityFactor);
                        SpawnSmokeExhaustTrail(countPerTick: count, speed: 2f * (1.2f - EasedLandingProgress));

                        if (EasedLandingProgress > 0.9f)
                            SpawnTileDust(closestTile, 2);

                        if (LandingProgress >= 1f - 0.03f)
                        {
                            SpawnTileDust(closestTile, 15);
                            SpawnHitTileDust(closestTile);
                            screenshakeIntensity = 20f * gravityFactor;
                        }

                        break;
                    }

                case ActionState.Docking:
                    {
                        lightIntensity = 10f;
                        screenshakeIntensity = DockingProgress < 0.05f ? 30f : 15f * (1f - Utility.QuadraticEaseOut(DockingProgress));

                        //int count = MacrocosmSubworld.CurrentAtmosphericDensity < 1f ? 2 : (int)(5f * atmoDesityFactor);
                        //SpawnSmokeExhaustTrail(countPerTick: count);

                        if (EasedFlightProgress < 0.1f)
                            SpawnTileDust(closestTile, 2);
                        break;
                    }


                case ActionState.Undocking:
                    {
                        lightIntensity = 10f;
                        screenshakeIntensity = 15f * (Utility.QuadraticEaseOut(UndockingProgress));
                        //int count = MacrocosmSubworld.CurrentAtmosphericDensity < 1f ? 2 : (int)(5f * atmoDesityFactor);
                        //SpawnSmokeExhaustTrail(countPerTick: count);

                        break;
                    }

                case ActionState.Idle:
                    break;
            }

            Lighting.AddLight(new Vector2(Center.X, Position.Y + Height + 15), new Color(197, 56, 9).ToVector3() * lightIntensity);

            Main.LocalPlayer.AddScreenshake(screenshakeIntensity, $"Rocket{WhoAmI}");

            // Hack to force render the tiles otherwise completetly unlighted, so the trail does not draw in front of them 
            for (int i = (int)(Position.X / 16f); i < (int)(Position.X / 16f) + Width / 16; i++)
                for (int j = (int)(Position.Y / 16f) + Height / 16 - 5; j < (int)(Position.Y / 16f) + Height / 16 + 15; j++)
                    Lighting.AddLight(new Vector2(i * 16, j * 16), new Vector3(0.01f));
        }

        private void SpawnSmokeExhaustTrail(int countPerTick, float speed = 1f)
        {
            for (int i = 0; i < countPerTick; i++)
            {
                Vector2 position = new Vector2(Center.X, Position.Y + Height - 28);

                Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(2, 10));
                if (State is ActionState.Landing or ActionState.Docking)
                    velocity = new(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(8, 16));

                var smoke = Particle.Create<RocketExhaustSmoke>(p =>
                {
                    p.Position = position;
                    p.Velocity = velocity * speed;
                    p.Scale = new(Main.rand.NextFloat(1.2f, 1.6f));
                    p.Rotation = 0f;
                    p.FadeIn = true;
                    p.FadeOut = true;
                    p.FadeInSpeed = 2;
                    p.FadeOutSpeed = 15;
                    p.TargetAlpha = 128;
                    p.ScaleDownSpeed = 0.001f;
                    p.Acceleration = new(0, -0.03f);
                    p.Color = Color.White.WithAlpha(150);
                    p.Collide = true;
                }, shouldSync: false);
            }

            int smallSmokeCount = (int)(countPerTick * 2f);

            var boosterLeft = AvailableModules.FirstOrDefault((m) => m is BoosterLeft) as Booster;
            var boosterRight = AvailableModules.FirstOrDefault((m) => m is BoosterRight) as Booster;

            for (int i = 0; i < smallSmokeCount; i++)
            {
                Vector2 position = i % 2 == 0 ?
                                new Vector2(boosterLeft.Position.X + boosterLeft.ExhaustOffsetX, Position.Y + Height - 28) :
                                new Vector2(boosterRight.Position.X + boosterRight.ExhaustOffsetX, Position.Y + Height - 28);


                Vector2 velocity = new(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(2, 4));
                if (State is ActionState.Landing or ActionState.Undocking)
                    velocity = new(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(8, 12));

                var smoke = Particle.Create<RocketExhaustSmoke>(p =>
                {
                    p.Position = position;
                    p.Velocity = velocity * speed;
                    p.Scale = new(Main.rand.NextFloat(0.6f, 0.9f));
                    p.Rotation = 0f;
                    p.FadeIn = true;
                    p.FadeOut = true;
                    p.FadeInSpeed = 2;
                    p.FadeOutSpeed = 12;
                    p.TargetAlpha = 128;
                    p.ScaleDownSpeed = 0.0015f;
                    p.Acceleration = new(0, -0.04f);
                    p.Color = Color.White.WithAlpha(150);
                    p.Collide = true;
                }, shouldSync: false);
            }
        }

        private void SpawnTileDust(Point tileCoords, int countPerTick)
        {
            for (int i = 0; i < countPerTick; i++)
            {
                var smoke = Particle.Create<Smoke>(p =>
                {
                    p.Position = tileCoords.ToWorldCoordinates() + new Vector2(0, 16);
                    p.Velocity = new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-0.1f, -1f));
                    p.Scale = new(Main.rand.NextFloat(0.5f, 1.2f));
                    p.Rotation = Utility.RandomRotation();
                    p.Color = Smoke.GetTileHitColor(tileCoords);
                    p.FadeIn = true;
                    p.Opacity = 0f;
                    p.ScaleVelocity = new(0.0075f);
                });
            }
        }

        private void SpawnHitTileDust(Point tileCoords)
        {
            for (int x = -8; x < 8; x++)
            {
                if (x is < -6 or > -2 and < 2 or > 6 && !Utility.CoordinatesOutOfBounds(tileCoords.X - x, tileCoords.Y))
                    WorldGen.KillTile_MakeTileDust(tileCoords.X - x, tileCoords.Y, Main.tile[tileCoords.X - x, tileCoords.Y]);
            }
        }

        public void PlaySound()
        {
            if (State == ActionState.StaticFire)
            {
                float intensity = 1f;
                SoundEngine.PlaySound(SFX.RocketLoop with
                {
                    Volume = intensity,
                    MaxInstances = 1,
                    SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
                },
                Center, updateCallback: (sound) =>
                {
                    sound.Pitch = MathHelper.Lerp(-1f, 0, StaticFireProgress);
                    sound.Volume = intensity;
                    sound.Position = Center;
                    return FlightTime < StaticFireDuration && ActiveInCurrentWorld;
                });
            }

            if (FlightTime == StaticFireDuration)
            {
                SoundEngine.PlaySound(SFX.RocketLaunch with
                {
                    Volume = 1f,
                    MaxInstances = 1,
                    SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
                },
                Center, updateCallback: (sound) =>
                {
                    sound.Position = Center;
                    return State == ActionState.Flight && ActiveInCurrentWorld;
                });
            }

            if (State is ActionState.Flight)
            {
                SoundEngine.PlaySound(SFX.RocketLoop with
                {
                    Volume = 1f,
                    IsLooped = true,
                    MaxInstances = 1,
                    SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
                },
                Center, updateCallback: (sound) =>
                {
                    sound.Pitch = Main.rand.NextFloat(-0.1f, 0.1f);
                    sound.Position = Center;
                    return State == ActionState.Flight && ActiveInCurrentWorld;
                });
            }

            if (State == ActionState.Landing)
            {
                SoundEngine.PlaySound(SFX.RocketLoop with
                {
                    MaxInstances = 1,
                    IsLooped = true,
                    Volume = 1f,
                    SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
                },
                Center, updateCallback: (sound) =>
                {
                    sound.Pitch = Main.rand.NextFloat(-0.1f, 0.1f);
                    sound.Volume = LandingProgress < 0.8f ? 1f : (1f - LandingProgress) * 5f;
                    sound.Position = Center;
                    return State == ActionState.Landing && ActiveInCurrentWorld;
                });
            }

            if (State == ActionState.Docking)
            {
                SoundEngine.PlaySound(SFX.RocketLoop with
                {
                    MaxInstances = 1,
                    IsLooped = true,
                    Volume = 1f,
                    SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
                },
                Center, updateCallback: (sound) =>
                {
                    sound.Pitch = Main.rand.NextFloat(-0.1f, 0.1f);
                    sound.Volume = DockingProgress < 0.8f ? 1f : (DockingProgress) * 5f;
                    sound.Position = Center;
                    return State == ActionState.Landing && ActiveInCurrentWorld;
                });
            }

            if (State == ActionState.Undocking)
            {
                SoundEngine.PlaySound(SFX.RocketLoop with
                {
                    MaxInstances = 1,
                    IsLooped = true,
                    Volume = 1f,
                    SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
                },
                Center, updateCallback: (sound) =>
                {
                    sound.Pitch = Main.rand.NextFloat(-0.1f, 0.1f);
                    sound.Volume = UndockingProgress < 0.8f ? 1f : (1f - UndockingProgress) * 5f;
                    sound.Position = Center;
                    return State == ActionState.Landing && ActiveInCurrentWorld;
                });
            }
        }

        // Handles the subworld travel, never on clients
        private void HandleTravel()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            bool samePlanet = CurrentWorld == TargetWorld;
            bool toParentPlanet = TargetIsParentSubworld(TargetWorld);

            CurrentWorld = TargetWorld;

            // Determine the landing location.
            // Set as default if no launchpad has been selected (i.e. the World Spawn option) 
            // For different world travel, the correct value is assigned when spawning in that world
            if (targetLaunchPad is not null)
                TargetTravelPosition = targetLaunchPad.CenterWorld + new Vector2(16, 16);
            else
                TargetTravelPosition = default;

            if (samePlanet)
            {
                // This failsafe logic could be extended to hiding the rocket for an amount of time, while remotely launching satellites
                // (no commander inside but wire triggered). Would mean also keeping the launchpad as occupied to avoid collisions on return
                if (!MacrocosmSubworld.IsValidID(TargetWorld))
                {
                    TargetTravelPosition = new(Center.X, StartPositionY + Height);
                    CurrentWorld = MacrocosmSubworld.CurrentID;
                }

                // For same world travel assign the correct position here
                if (TargetTravelPosition == default)
                    TargetTravelPosition = Utility.SpawnWorldPosition + new Vector2(0, 16f * 2);

                Center = new(TargetTravelPosition.X, Center.Y);
            }
            else
            {
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    if (CheckPlayerInRocket(Main.myPlayer))
                        SubworldTravelPlayer.Travel(TargetWorld, this, downwards: toParentPlanet);
                }
                else
                {
                    foreach (Player player in Main.ActivePlayers)
                    {
                        if (CheckPlayerInRocket(player.whoAmI))
                        {
                            SubworldTravelPlayer.SendTravelRequest(player.whoAmI, TargetWorld, WhoAmI);
                        }
                    }
                }
            }
        }
    }
}
