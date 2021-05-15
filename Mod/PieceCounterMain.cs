using System;
using System.Threading;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace CampaignMod
{
    [BepInPlugin(pluginGuid, pluginName, pluginVerson)]
    [BepInProcess("Poly Bridge 2")]
    public class PieceCounterMain : BaseUnityPlugin
    {

        public const string pluginGuid = "polytech.piececountermod";

        public const string pluginName = "Piece Counter Mod";

        public const string pluginVerson = "1.0.0";

        public ConfigDefinition modEnableDef = new ConfigDefinition(pluginName, "Enable/Disable Mod");
        public ConfigDefinition DigitsDef = new ConfigDefinition(pluginName, "Digits");
        public ConfigDefinition OnTopDef = new ConfigDefinition(pluginName, "Always On Top");
        public ConfigDefinition ResetDef = new ConfigDefinition(pluginName, "Reset Counter");

        public ConfigDefinition RoadDef = new ConfigDefinition(pluginName, "Road");
        public ConfigDefinition RRoadDef = new ConfigDefinition(pluginName, "Reinforced Road");
        public ConfigDefinition WoodDef = new ConfigDefinition(pluginName, "Wood");
        public ConfigDefinition SteelDef = new ConfigDefinition(pluginName, "Steel");
        public ConfigDefinition HydroDef = new ConfigDefinition(pluginName, "Hydraulics");
        public ConfigDefinition RopeDef = new ConfigDefinition(pluginName, "Rope");
        public ConfigDefinition CableDef = new ConfigDefinition(pluginName, "Cable");
        public ConfigDefinition SpringDef = new ConfigDefinition(pluginName, "Spring");

        public ConfigEntry<bool> mEnabled;
        public ConfigEntry<int> mDigits;
        public ConfigEntry<bool> mOnTop;
        public ConfigEntry<KeyboardShortcut> mReset;

        public ConfigEntry<int> mRoad;
        public ConfigEntry<int> mRRoad;
        public ConfigEntry<int> mWood;
        public ConfigEntry<int> mSteel;
        public ConfigEntry<int> mHydro;
        public ConfigEntry<int> mRope;
        public ConfigEntry<int> mCable;
        public ConfigEntry<int> mSpring;

        public Socket CounterSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public static PieceCounterMain instance;

        public Stopwatch sw = new Stopwatch();

        public bool SkipCounting = false;

        void Awake()
        {
            if (instance == null) instance = this;

            int order = 0;

            mEnabled = Config.Bind(modEnableDef, true, new ConfigDescription("Controls if the mod should be enabled or disabled", null, new ConfigurationManagerAttributes { Order = order }));
            mEnabled.SettingChanged += onEnableDisable;
            order--;

            mDigits = Config.Bind(DigitsDef, 3, new ConfigDescription("The amount of digits on the display", null, new ConfigurationManagerAttributes { Order = order }));
            mDigits.SettingChanged += onDigitChanged;
            order--;

            mOnTop = Config.Bind(OnTopDef, false, new ConfigDescription("Controls if the application will always be displayed on top", null, new ConfigurationManagerAttributes { Order = order }));
            mOnTop.SettingChanged += onOnTopChanged;
            order--;

            mReset = Config.Bind(ResetDef, new KeyboardShortcut(KeyCode.None), new ConfigDescription("The key that will reset the counters", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            mRoad = Config.Bind(RoadDef, 0, new ConfigDescription("The amount of road", null, new ConfigurationManagerAttributes { Order = order }));
            order--;
            mRRoad = Config.Bind(RRoadDef, 0, new ConfigDescription("The amount of reinforced road", null, new ConfigurationManagerAttributes { Order = order }));
            order--;
            mWood = Config.Bind(WoodDef, 0, new ConfigDescription("The amount of wood", null, new ConfigurationManagerAttributes { Order = order }));
            order--;
            mSteel = Config.Bind(SteelDef, 0, new ConfigDescription("The amount of steel", null, new ConfigurationManagerAttributes { Order = order }));
            order--;
            mHydro = Config.Bind(HydroDef, 0, new ConfigDescription("The amount of hydraulics", null, new ConfigurationManagerAttributes { Order = order }));
            order--;
            mRope = Config.Bind(RopeDef, 0, new ConfigDescription("The amount of rope", null, new ConfigurationManagerAttributes { Order = order }));
            order--;
            mCable = Config.Bind(CableDef, 0, new ConfigDescription("The amount of cable", null, new ConfigurationManagerAttributes { Order = order }));
            order--;
            mSpring = Config.Bind(SpringDef, 0, new ConfigDescription("The amount of spring", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            Config.SettingChanged += onSettingChanged;
            onEnableDisable(null, null);
            onSettingChanged(null, null);

            Harmony harmony = new Harmony(pluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

        }

        public void onEnableDisable(object sender, EventArgs ev)
        {
            if (mEnabled.Value)
            {
                CounterSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, 23232);
                    CounterSocket.Connect(remoteEP);
                    Debug.Log("Connected to application server");

                    nums = new int[8];
                    sendDigitData(mDigits.Value);
                    sendOnTopData(mOnTop.Value);
                }
                catch (Exception e)
                {
                    instance.mEnabled.Value = false;
                    Debug.Log("Something went wrong while trying to connect to the piece counter application: " + e.ToString());
                    if (GameUI.m_Instance != null) PopUpWarning.Display("Could not connect to the application!\nDisabled the mod");
                }
            }
            else
            {
                if (CounterSocket.Connected)
                {
                    sendClose();
                    CounterSocket.Close();
                }
                CounterSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
        }

        public void onDigitChanged(object sender, EventArgs ev)
        {
            if (CheckForCheating() && CounterSocket.Connected) sendDigitData(mDigits.Value);
        }

        public void onOnTopChanged(object sender, EventArgs ev)
        {
            if (CheckForCheating() && CounterSocket.Connected) sendOnTopData(mOnTop.Value);
        }

        public void onSettingChanged(object sender, EventArgs ev)
        {
            if (!CheckForCheating()) return;
        }

        private bool CheckForCheating()
        {
            return mEnabled.Value;
        }

        int[] nums = new int[8];

        void Update()
        {
            if (!CheckForCheating()) return;

            if (mReset.Value.IsDown())
            {
                mRoad.Value = 0;
                mRRoad.Value = 0;
                mWood.Value = 0;
                mSteel.Value = 0;
                mHydro.Value = 0;
                mRope.Value = 0;
                mCable.Value = 0;
                mSpring.Value = 0;
            }

            byte i = 0;
            if (nums[i] != (nums[i] = mRoad.Value)) sendCountData(i, mRoad.Value); i++;
            if (nums[i] != (nums[i] = mRRoad.Value)) sendCountData(i, mRRoad.Value); i++;
            if (nums[i] != (nums[i] = mWood.Value)) sendCountData(i, mWood.Value); i++;
            if (nums[i] != (nums[i] = mSteel.Value)) sendCountData(i, mSteel.Value); i++;
            if (nums[i] != (nums[i] = mHydro.Value)) sendCountData(i, mHydro.Value); i++;
            if (nums[i] != (nums[i] = mRope.Value)) sendCountData(i, mRope.Value); i++;
            if (nums[i] != (nums[i] = mCable.Value)) sendCountData(i, mCable.Value); i++;
            if (nums[i] != (nums[i] = mSpring.Value)) sendCountData(i, mSpring.Value);

            if (!sw.IsRunning)
            {
                sw.Start();
            }
            else if (sw.ElapsedMilliseconds > 5000)
            {
                sendPing();
                sw.Restart();
            }

            SkipCounting = false;
        }


        [HarmonyPatch(typeof(BridgeActions), "Create")]
        [HarmonyPatch(new Type[] { typeof(BridgeEdge) })]
        private static class patchCreateEdge
        {
            private static void Postfix(BridgeEdge edge)
            {
                if (!instance.CheckForCheating()) return;

                instance.addCount(edge.m_Material.m_MaterialType);
            }
        }
        
        [HarmonyPatch(typeof(BridgeActions), "Create")]
        [HarmonyPatch(new Type[] { typeof(List<BridgeEdge>) })]
        private static class patchCreateEdgeList
        {
            private static void Postfix(List<BridgeEdge> edges)
            {
                if (!instance.CheckForCheating()) return;

                foreach (BridgeEdge edge in edges) instance.addCount(edge.m_Material.m_MaterialType);
            }
        }

        [HarmonyPatch(typeof(BridgeActions), "Delete")]
        [HarmonyPatch(new Type[] { typeof(BridgeEdge) })]
        private static class patchDeleteEdge
        {
            private static void Postfix(BridgeEdge edge)
            {
                if (!instance.CheckForCheating()) return;

                instance.removeCount(edge.m_Material.m_MaterialType);
            }
        }

        [HarmonyPatch(typeof(BridgeActions), "Delete")]
        [HarmonyPatch(new Type[] { typeof(List<BridgeEdge>) })]
        private static class patchDeleteEdgeList
        {
            private static void Postfix(List<BridgeEdge> edges)
            {
                if (!instance.CheckForCheating()) return;

                foreach (BridgeEdge edge in edges) instance.removeCount(edge.m_Material.m_MaterialType);
            }
        }

        [HarmonyPatch(typeof(BridgeActions), "Delete")]
        [HarmonyPatch(new Type[] { typeof(HashSet<BridgeEdge>) })]
        private static class patchDeleteEdgeHashSet
        {
            private static void Postfix(HashSet<BridgeEdge> edges)
            {
                if (!instance.CheckForCheating()) return;

                foreach (BridgeEdge edge in edges) instance.removeCount(edge.m_Material.m_MaterialType);
            }
        }

        [HarmonyPatch(typeof(BridgeUndo), "UndoCreateEdge")]
        private static class patchUndoCreate
        {
            private static void Postfix(BridgeActionPacket packet)
            {
                if (!instance.CheckForCheating()) return;

                instance.removeCount(packet.m_Edge.m_Material);
            }
        }

        [HarmonyPatch(typeof(BridgeUndo), "UndoDeleteEdge")]
        private static class patchUndoDelete
        {
            private static void Postfix(BridgeActionPacket packet)
            {
                if (!instance.CheckForCheating()) return;

                instance.addCount(packet.m_Edge.m_Material);
            }
        }


        [HarmonyPatch(typeof(BridgeSave), "Deserialize")]
        private static class patchBridgeDeserialize
        {
            private static void Postfix(BridgeSaveData saveData)
            {
                if (!instance.CheckForCheating() || instance.SkipCounting) return;

                foreach (BridgeEdgeProxy edgeProxy in saveData.m_BridgeEdges) instance.addCount(edgeProxy.m_Material);
            }
        }
        
        [HarmonyPatch(typeof(GameStateMainMenu), "LoadLayout")]
        private static class patchLoadLayoutMainMenu
        {
            private static void Prefix()
            {
                if (!instance.CheckForCheating()) return;

                instance.SkipCounting = true;
            }
        }

        [HarmonyPatch(typeof(Bridge), "RevertToStateBeforeSimulation")]
        private static class patchLoadBridgeBeforeSim
        {
            private static void Prefix()
            {
                if (!instance.CheckForCheating()) return;

                instance.SkipCounting = true;
            }
        }


        public void addCount(BridgeMaterialType type)
        {
            switch (type)
            {
                case BridgeMaterialType.ROAD:
                    instance.mRoad.Value++;
                    break;
                case BridgeMaterialType.REINFORCED_ROAD:
                    instance.mRRoad.Value++;
                    break;
                case BridgeMaterialType.WOOD:
                    instance.mWood.Value++;
                    break;
                case BridgeMaterialType.STEEL:
                    instance.mSteel.Value++;
                    break;
                case BridgeMaterialType.HYDRAULICS:
                    instance.mHydro.Value++;
                    break;
                case BridgeMaterialType.ROPE:
                    instance.mRope.Value++;
                    break;
                case BridgeMaterialType.CABLE:
                    instance.mCable.Value++;
                    break;
                case BridgeMaterialType.SPRING:
                    instance.mSpring.Value++;
                    break;
                case BridgeMaterialType.BUNGINE_ROPE:
                case BridgeMaterialType.INVALID:
                default:
                    break;
            }
        }

        public void removeCount(BridgeMaterialType type)
        {
            switch (type)
            {
                case BridgeMaterialType.ROAD:
                    if (instance.mRoad.Value > 0) instance.mRoad.Value--;
                    break;
                case BridgeMaterialType.REINFORCED_ROAD:
                    if (instance.mRRoad.Value > 0) instance.mRRoad.Value--;
                    break;
                case BridgeMaterialType.WOOD:
                    if (instance.mWood.Value > 0) instance.mWood.Value--;
                    break;
                case BridgeMaterialType.STEEL:
                    if (instance.mSteel.Value > 0) instance.mSteel.Value--;
                    break;
                case BridgeMaterialType.HYDRAULICS:
                    if (instance.mHydro.Value > 0) instance.mHydro.Value--;
                    break;
                case BridgeMaterialType.ROPE:
                    if (instance.mRope.Value > 0) instance.mRope.Value--;
                    break;
                case BridgeMaterialType.CABLE:
                    if (instance.mCable.Value > 0) instance.mCable.Value--;
                    break;
                case BridgeMaterialType.SPRING:
                    if (instance.mSpring.Value > 0) instance.mSpring.Value--;
                    break;
                case BridgeMaterialType.BUNGINE_ROPE:
                case BridgeMaterialType.INVALID:
                default:
                    break;
            }
        }


        public void sendCountData(byte index, int count)
        {
            new Thread(new ThreadStart(delegate 
            {
                if (!instance.CounterSocket.Connected)
                {
                    return;
                }
                else if (index >= 8)
                {
                    Debug.Log("Invalid index!");
                    return;
                }
                byte[] data = new byte[6];
                data[0] = 1;
                data[1] = index;
                BitConverter.GetBytes(count).CopyTo(data, 2);

                try
                {
                    CounterSocket.Send(data);
                }
                catch (Exception e)
                {
                    Debug.Log("Something went wrong while trying to send data!\n" + e.ToString());
                }
            })).Start();
        }

        public void sendDigitData(int count)
        {
            if (!instance.CounterSocket.Connected)
            {
                return;
            }
            else if (count > 10)
            {
                mDigits.Value = 10;
                return;
            }
            else if (count < 0)
            {
                mDigits.Value = 0;
                return;
            }
            byte[] data = new byte[2];
            data[0] = 2;
            data[1] = (byte)count;

            try
            {
                CounterSocket.Send(data);
            }
            catch (Exception e)
            {
                Debug.Log("Something went wrong while trying to send data!\n" + e.ToString());
            }
        }

        public void sendOnTopData(bool flag)
        {
            if (!instance.CounterSocket.Connected)
            {
                return;
            }
            byte[] data = new byte[2];
            data[0] = 3;
            if (flag) data[1] = 1;
            else data[1] = 0;

            try
            {
                CounterSocket.Send(data);
            }
            catch (Exception e)
            {
                Debug.Log("Something went wrong while trying to send data!\n" + e.ToString());
            }
        }

        public void sendPing()
        {
            if (!instance.CounterSocket.Connected)
            {
                instance.mEnabled.Value = false;
                if (GameUI.m_Instance != null) PopUpWarning.Display("Connection with application has been lost!\nDisabled the mod");
                CounterSocket.Close();
                return;
            }
            byte[] data = new byte[1];
            data[0] = 15;

            try
            {
                CounterSocket.Send(data);
            }
            catch (Exception e)
            {
                Debug.Log("Something went wrong while trying to send data!\n" + e.ToString());
                instance.mEnabled.Value = false;
                if (GameUI.m_Instance != null) PopUpWarning.Display("Connection with application has been lost!\nDisabled the mod");
                CounterSocket.Close();
            }
        }

        public void sendClose()
        {
            if (!instance.CounterSocket.Connected)
            {
                return;
            }
            byte[] data = new byte[1];
            data[0] = 16;

            try
            {
                CounterSocket.Send(data);
            }
            catch (Exception e)
            {
                Debug.Log("Something went wrong while trying to send data!\n" + e.ToString());
            }
            CounterSocket.Close();
        }
    }



    /// <summary>
    /// Class that specifies how a setting should be displayed inside the ConfigurationManager settings window.
    /// 
    /// Usage:
    /// This class template has to be copied inside the plugin's project and referenced by its code directly.
    /// make a new instance, assign any fields that you want to override, and pass it as a tag for your setting.
    /// 
    /// If a field is null (default), it will be ignored and won't change how the setting is displayed.
    /// If a field is non-null (you assigned a value to it), it will override default behavior.
    /// </summary>
    /// 
    /// <example> 
    /// Here's an example of overriding order of settings and marking one of the settings as advanced:
    /// <code>
    /// // Override IsAdvanced and Order
    /// Config.AddSetting("X", "1", 1, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 3 }));
    /// // Override only Order, IsAdvanced stays as the default value assigned by ConfigManager
    /// Config.AddSetting("X", "2", 2, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 1 }));
    /// Config.AddSetting("X", "3", 3, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 2 }));
    /// </code>
    /// </example>
    /// 
    /// <remarks> 
    /// You can read more and see examples in the readme at https://github.com/BepInEx/BepInEx.ConfigurationManager
    /// You can optionally remove fields that you won't use from this class, it's the same as leaving them null.
    /// </remarks>
#pragma warning disable 0169, 0414, 0649
    internal sealed class ConfigurationManagerAttributes
    {
        /// <summary>
        /// Should the setting be shown as a percentage (only use with value range settings).
        /// </summary>
        public bool? ShowRangeAsPercent;

        /// <summary>
        /// Custom setting editor (OnGUI code that replaces the default editor provided by ConfigurationManager).
        /// See below for a deeper explanation. Using a custom drawer will cause many of the other fields to do nothing.
        /// </summary>
        public System.Action<BepInEx.Configuration.ConfigEntryBase> CustomDrawer;

        /// <summary>
        /// Show this setting in the settings screen at all? If false, don't show.
        /// </summary>
        public bool? Browsable;

        /// <summary>
        /// Category the setting is under. Null to be directly under the plugin.
        /// </summary>
        public string Category;

        /// <summary>
        /// If set, a "Default" button will be shown next to the setting to allow resetting to default.
        /// </summary>
        public object DefaultValue;

        /// <summary>
        /// Force the "Reset" button to not be displayed, even if a valid DefaultValue is available. 
        /// </summary>
        public bool? HideDefaultButton;

        /// <summary>
        /// Force the setting name to not be displayed. Should only be used with a <see cref="CustomDrawer"/> to get more space.
        /// Can be used together with <see cref="HideDefaultButton"/> to gain even more space.
        /// </summary>
        public bool? HideSettingName;

        /// <summary>
        /// Optional description shown when hovering over the setting.
        /// Not recommended, provide the description when creating the setting instead.
        /// </summary>
        public string Description;

        /// <summary>
        /// Name of the setting.
        /// </summary>
        public string DispName;

        /// <summary>
        /// Order of the setting on the settings list relative to other settings in a category.
        /// 0 by default, higher number is higher on the list.
        /// </summary>
        public int? Order;

        /// <summary>
        /// Only show the value, don't allow editing it.
        /// </summary>
        public bool? ReadOnly;

        /// <summary>
        /// If true, don't show the setting by default. User has to turn on showing advanced settings or search for it.
        /// </summary>
        public bool? IsAdvanced;

        /// <summary>
        /// Custom converter from setting type to string for the built-in editor textboxes.
        /// </summary>
        public System.Func<object, string> ObjToStr;

        /// <summary>
        /// Custom converter from string to setting type for the built-in editor textboxes.
        /// </summary>
        public System.Func<string, object> StrToObj;
    }
}
