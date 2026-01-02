using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace WindowsOptimizer.Infrastructure.Metrics;

[SupportedOSPlatform("windows")]
public sealed class WifiSignalMonitor : IDisposable
{
    private IntPtr _clientHandle;
    private bool _isAvailable;

    public WifiSignalMonitor()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var result = NativeMethods.WlanOpenHandle(2, IntPtr.Zero, out _, out _clientHandle);
        _isAvailable = result == 0 && _clientHandle != IntPtr.Zero;
    }

    public WifiSignalSnapshot? TryGetSignal()
    {
        if (!_isAvailable)
        {
            return null;
        }

        IntPtr interfaceListPtr = IntPtr.Zero;
        IntPtr dataPtr = IntPtr.Zero;

        try
        {
            var result = NativeMethods.WlanEnumInterfaces(_clientHandle, IntPtr.Zero, out interfaceListPtr);
            if (result != 0 || interfaceListPtr == IntPtr.Zero)
            {
                return null;
            }

            foreach (var iface in NativeMethods.ParseInterfaceList(interfaceListPtr))
            {
                if (iface.State != WLAN_INTERFACE_STATE.wlan_interface_state_connected)
                {
                    continue;
                }

                var interfaceGuid = iface.InterfaceGuid;
                var queryResult = NativeMethods.WlanQueryInterface(
                    _clientHandle,
                    ref interfaceGuid,
                    WLAN_INTF_OPCODE.wlan_intf_opcode_current_connection,
                    IntPtr.Zero,
                    out _,
                    out dataPtr,
                    out _);

                if (queryResult != 0 || dataPtr == IntPtr.Zero)
                {
                    continue;
                }

                var attributes = Marshal.PtrToStructure<WLAN_CONNECTION_ATTRIBUTES>(dataPtr);
                var ssid = attributes.wlanAssociationAttributes.dot11Ssid.GetSsid();
                var quality = (int)attributes.wlanAssociationAttributes.wlanSignalQuality;

                return new WifiSignalSnapshot(ssid, quality);
            }
        }
        catch
        {
        }
        finally
        {
            if (dataPtr != IntPtr.Zero)
            {
                NativeMethods.WlanFreeMemory(dataPtr);
            }

            if (interfaceListPtr != IntPtr.Zero)
            {
                NativeMethods.WlanFreeMemory(interfaceListPtr);
            }
        }

        return null;
    }

    public void Dispose()
    {
        if (_clientHandle != IntPtr.Zero)
        {
            NativeMethods.WlanCloseHandle(_clientHandle, IntPtr.Zero);
            _clientHandle = IntPtr.Zero;
        }

        _isAvailable = false;
    }

    private static class NativeMethods
    {
        [DllImport("wlanapi.dll", SetLastError = true)]
        public static extern uint WlanOpenHandle(uint dwClientVersion, IntPtr pReserved, out uint pdwNegotiatedVersion, out IntPtr phClientHandle);

        [DllImport("wlanapi.dll", SetLastError = true)]
        public static extern uint WlanCloseHandle(IntPtr hClientHandle, IntPtr pReserved);

        [DllImport("wlanapi.dll", SetLastError = true)]
        public static extern uint WlanEnumInterfaces(IntPtr hClientHandle, IntPtr pReserved, out IntPtr ppInterfaceList);

        [DllImport("wlanapi.dll", SetLastError = true)]
        public static extern uint WlanQueryInterface(
            IntPtr hClientHandle,
            ref Guid pInterfaceGuid,
            WLAN_INTF_OPCODE OpCode,
            IntPtr pReserved,
            out int pdwDataSize,
            out IntPtr ppData,
            out WLAN_OPCODE_VALUE_TYPE pWlanOpcodeValueType);

        [DllImport("wlanapi.dll", SetLastError = true)]
        public static extern void WlanFreeMemory(IntPtr pMemory);

        public static WLAN_INTERFACE_INFO[] ParseInterfaceList(IntPtr listPtr)
        {
            var numberOfItems = Marshal.ReadInt32(listPtr, 0);
            var listHeaderSize = 8;
            var itemSize = Marshal.SizeOf<WLAN_INTERFACE_INFO>();
            var list = new WLAN_INTERFACE_INFO[numberOfItems];
            var current = IntPtr.Add(listPtr, listHeaderSize);

            for (var i = 0; i < numberOfItems; i++)
            {
                list[i] = Marshal.PtrToStructure<WLAN_INTERFACE_INFO>(current);
                current = IntPtr.Add(current, itemSize);
            }

            return list;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WLAN_INTERFACE_INFO
    {
        public Guid InterfaceGuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Description;
        public WLAN_INTERFACE_STATE State;
    }

    private enum WLAN_INTERFACE_STATE
    {
        wlan_interface_state_not_ready = 0,
        wlan_interface_state_connected = 1,
        wlan_interface_state_ad_hoc_network_formed = 2,
        wlan_interface_state_disconnecting = 3,
        wlan_interface_state_disconnected = 4,
        wlan_interface_state_associating = 5,
        wlan_interface_state_discovering = 6,
        wlan_interface_state_authenticating = 7
    }

    private enum WLAN_INTF_OPCODE
    {
        wlan_intf_opcode_autoconf_enabled = 1,
        wlan_intf_opcode_bss_type = 2,
        wlan_intf_opcode_interface_state = 3,
        wlan_intf_opcode_current_connection = 7
    }

    private enum WLAN_OPCODE_VALUE_TYPE
    {
        wlan_opcode_value_type_query_only = 0,
        wlan_opcode_value_type_set_by_group_policy = 1,
        wlan_opcode_value_type_set_by_user = 2,
        wlan_opcode_value_type_invalid = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WLAN_CONNECTION_ATTRIBUTES
    {
        public WLAN_INTERFACE_STATE isState;
        public WLAN_CONNECTION_MODE wlanConnectionMode;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strProfileName;
        public WLAN_ASSOCIATION_ATTRIBUTES wlanAssociationAttributes;
        public WLAN_SECURITY_ATTRIBUTES wlanSecurityAttributes;
    }

    private enum WLAN_CONNECTION_MODE
    {
        wlan_connection_mode_profile = 0,
        wlan_connection_mode_temporary_profile = 1,
        wlan_connection_mode_discovery_secure = 2,
        wlan_connection_mode_discovery_unsecure = 3,
        wlan_connection_mode_auto = 4,
        wlan_connection_mode_invalid = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WLAN_ASSOCIATION_ATTRIBUTES
    {
        public DOT11_SSID dot11Ssid;
        public DOT11_BSS_TYPE dot11BssType;
        public DOT11_MAC_ADDRESS dot11Bssid;
        public DOT11_PHY_TYPE dot11PhyType;
        public uint uDot11PhyIndex;
        public uint wlanSignalQuality;
        public uint ulRxRate;
        public uint ulTxRate;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DOT11_SSID
    {
        public uint uSSIDLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] ucSSID;

        public string GetSsid()
        {
            if (ucSSID == null || uSSIDLength == 0)
            {
                return string.Empty;
            }

            return Encoding.ASCII.GetString(ucSSID, 0, (int)uSSIDLength);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DOT11_MAC_ADDRESS
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] Address;
    }

    private enum DOT11_BSS_TYPE
    {
        dot11_BSS_type_infrastructure = 1,
        dot11_BSS_type_independent = 2,
        dot11_BSS_type_any = 3
    }

    private enum DOT11_PHY_TYPE
    {
        dot11_phy_type_unknown = 0,
        dot11_phy_type_any = 0,
        dot11_phy_type_fhss = 1,
        dot11_phy_type_dsss = 2,
        dot11_phy_type_irbaseband = 3,
        dot11_phy_type_ofdm = 4,
        dot11_phy_type_hrdsss = 5,
        dot11_phy_type_erp = 6,
        dot11_phy_type_ht = 7,
        dot11_phy_type_vht = 8,
        dot11_phy_type_dmg = 9,
        dot11_phy_type_he = 10,
        dot11_phy_type_eht = 11
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WLAN_SECURITY_ATTRIBUTES
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool bSecurityEnabled;
        [MarshalAs(UnmanagedType.Bool)]
        public bool bOneXEnabled;
        public DOT11_AUTH_ALGORITHM dot11AuthAlgorithm;
        public DOT11_CIPHER_ALGORITHM dot11CipherAlgorithm;
    }

    private enum DOT11_AUTH_ALGORITHM : uint
    {
        DOT11_AUTH_ALGO_80211_OPEN = 1,
        DOT11_AUTH_ALGO_80211_SHARED_KEY = 2,
        DOT11_AUTH_ALGO_WPA = 3,
        DOT11_AUTH_ALGO_WPA_PSK = 4,
        DOT11_AUTH_ALGO_WPA_NONE = 5,
        DOT11_AUTH_ALGO_RSNA = 6,
        DOT11_AUTH_ALGO_RSNA_PSK = 7,
        DOT11_AUTH_ALGO_IHV_START = 0x80000000,
        DOT11_AUTH_ALGO_IHV_END = 0xffffffff
    }

    private enum DOT11_CIPHER_ALGORITHM : uint
    {
        DOT11_CIPHER_ALGO_NONE = 0x00,
        DOT11_CIPHER_ALGO_WEP40 = 0x01,
        DOT11_CIPHER_ALGO_TKIP = 0x02,
        DOT11_CIPHER_ALGO_CCMP = 0x04,
        DOT11_CIPHER_ALGO_WEP104 = 0x05,
        DOT11_CIPHER_ALGO_WPA_USE_GROUP = 0x100,
        DOT11_CIPHER_ALGO_RSN_USE_GROUP = 0x100,
        DOT11_CIPHER_ALGO_WEP = 0x101,
        DOT11_CIPHER_ALGO_IHV_START = 0x80000000,
        DOT11_CIPHER_ALGO_IHV_END = 0xffffffff
    }
}

public readonly record struct WifiSignalSnapshot(string Ssid, int SignalQuality);
