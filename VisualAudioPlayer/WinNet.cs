using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Net;
using System.Windows.Forms;
using System.Security.Permissions;

namespace VisualAudioPlayer
{
	public class WinNet
	{
		#region Consts
		const int RESOURCE_CONNECTED = 0x00000001;
		const int RESOURCE_GLOBALNET = 0x00000002;
		const int RESOURCE_REMEMBERED = 0x00000003;

		const int RESOURCETYPE_ANY = 0x00000000;
		const int RESOURCETYPE_DISK = 0x00000001;
		const int RESOURCETYPE_PRINT = 0x00000002;

		const int RESOURCEDISPLAYTYPE_GENERIC = 0x00000000;
		const int RESOURCEDISPLAYTYPE_DOMAIN = 0x00000001;
		const int RESOURCEDISPLAYTYPE_SERVER = 0x00000002;
		const int RESOURCEDISPLAYTYPE_SHARE = 0x00000003;
		const int RESOURCEDISPLAYTYPE_FILE = 0x00000004;
		const int RESOURCEDISPLAYTYPE_GROUP = 0x00000005;

		const int RESOURCEUSAGE_CONNECTABLE = 0x00000001;
		const int RESOURCEUSAGE_CONTAINER = 0x00000002;


		const int CONNECT_INTERACTIVE = 0x00000008;//operating system may interact with the user for authentication purposes.
		const int CONNECT_PROMPT = 0x00000010;//not to use any default settings for user names or passwords without offering the user the opportunity to supply an alternative
		const int CONNECT_REDIRECT = 0x00000080;//forces the redirection of a local device
		const int CONNECT_UPDATE_PROFILE = 0x00000001;//instructs the operating system to store the network resource connection
		const int CONNECT_COMMANDLINE = 0x00000800;//using the command line instead of a graphical user interface 
		const int CONNECT_CMD_SAVECRED = 0x00001000;//credential should be saved by the credential manager

		const int CONNECT_LOCALDRIVE = 0x00000100;
		#endregion
		#region Errors
		public const int NO_ERROR = 0;
		public const int ERROR_ACCESS_DENIED = 5;
		public const int ERROR_ALREADY_ASSIGNED = 85;
		public const int ERROR_BAD_DEV_TYPE = 66;
		public const int ERROR_BAD_DEVICE = 1200;
		public const int ERROR_BAD_NET_NAME = 67;
		public const int ERROR_BAD_PROVIDER = 1204;
		public const int ERROR_BAD_USERNAME = 2202;
		public const int ERROR_BUSY = 170;
		public const int ERROR_CANCELLED = 1223;
		public const int ERROR_CANNOT_OPEN_PROFILE = 1205;
		public const int ERROR_DEVICE_ALREADY_REMEMBERED = 1202;
		public const int ERROR_EXTENDED_ERROR = 1208;
		public const int ERROR_INVALID_ADDRESS = 487;
		public const int ERROR_INVALID_PARAMETER = 87;
		public const int ERROR_INVALID_PASSWORD = 1216;
		public const int ERROR_LOGON_FAILURE = 1326;
		public const int ERROR_NO_NET_OR_BAD_PATH = 1203;
		public const int ERROR_MORE_DATA = 234;
		public const int ERROR_NO_MORE_ITEMS = 259;
		public const int ERROR_NO_NETWORK = 1222;
		public const int ERROR_BAD_PROFILE = 1206;
		public const int ERROR_DEVICE_IN_USE = 2404;
		public const int ERROR_NOT_CONNECTED = 2250;
		public const int ERROR_OPEN_FILES = 2401;

		public struct Error
		{
			public int num;
			public string message;
			public Error(int num, string message)
			{
				this.num = num;
				this.message = message;
			}
		}
		#endregion
		#region DllImport

		[StructLayout(LayoutKind.Sequential)]
		public class NetResource
		{
			public ResourceScope Scope;
			public ResourceType ResourceType;
			public ResourceDisplaytype DisplayType;
			public int Usage;
			public string LocalName;
			public string RemoteName;
			public string Comment;
			public string Provider;
		}
		public enum ResourceScope : int
		{
			Connected = 1,
			GlobalNetwork,
			Remembered,
			Recent,
			Context
		};

		public enum ResourceType : int
		{
			Any = 0,
			Disk = 1,
			Print = 2,
			Reserved = 8,
		}

		public enum ResourceDisplaytype : int
		{
			Generic = 0x0,
			Domain = 0x01,
			Server = 0x02,
			Share = 0x03,
			File = 0x04,
			Group = 0x05,
			Network = 0x06,
			Root = 0x07,
			Shareadmin = 0x08,
			Directory = 0x09,
			Tree = 0x0a,
			Ndscontainer = 0x0b
		}
		[DllImport("mpr.dll")]
		private static extern int WNetAddConnection2(NetResource netResource, string password, string username, int flags);

		[DllImport("Mpr.dll")]
		private static extern int WNetUseConnection(
			IntPtr hwndOwner,
			NetResource lpNetResource,
			string lpPassword,
			string lpUserID,
			int dwFlags,
			string lpAccessName,
			string lpBufferSize,
			string lpResult
			);
		[DllImport("mpr", CharSet = CharSet.Auto)]
		private static extern int WNetGetUniversalName(string lpLocalPath, int dwInfoLevel, IntPtr lpBuffer, ref int lpBufferSize);
		[DllImport("mpr.dll")]
		private static extern int WNetUseConnection(IntPtr hwndOwner, ref NetResource lpNetResource, string lpPassword, string lpUserId, int dwFlags, string lpAccessName, string lpBufferSize, string lpResult);
		[DllImport("mpr.dll")]
		private static extern int WNetCancelConnection2(string name, int flags, bool force);
		public enum NetworkType : ushort
		{
			MSNET = 0x0001,
			LANMAN = 0x0002,
			NETWARE = 0x0003,
			VINES = 0x0004,
			NET10 = 0x0005,
			LOCUS = 0x0006,
			SUN_PC_NFS = 0x0007,
			LANSTEP = 0x0008,
			TITLES_9 = 0x0009,
			LANTASTIC = 0x000A,
			AS400 = 0x000B,
			FTP_NFS = 0x000C,
			PATHWORKS = 0x000D,
			LIFENET = 0x000E,
			POWERLAN = 0x000F,
			BWNFS = 0x0010,
			COGENT = 0x0011,
			FARALLON = 0x0012,
			APPLETALK = 0x0013,
			INTERGRAPH = 0x0014,
			SYMFONET = 0x0015,
			CLEARCASE = 0x0016,
			FRONTIER = 0x0017,
			BMC = 0x0018,
			DCE = 0x0019,
			AVID = 0x001A,
			DOCUSPACE = 0x001B,
			MANGOSOFT = 0x001C,
			SERNET = 0x001D,
			RIVERFRONT1 = 0x001E,
			RIVERFRONT2 = 0x001F,
			DECORB = 0x0020,
			PROTSTOR = 0x0021,
			FJ_REDIR = 0x0022,
			DISTINCT = 0x0023,
			TWINS = 0x0024,
			RDR2SAMPLE = 0x0025,
			CSC = 0x0026,
			IN31 = 0x0027,
			EXTENDNET = 0x0029,
			STAC = 0x002A,
			FOXBAT = 0x002B,
			YAHOO = 0x002C,
			EXIFS = 0x002D,
			DAV = 0x002E,
			KNOWARE = 0x002F,
			OBJECT_DIRE = 0x0030,
			MASFAX = 0x0031,
			HOB_NFS = 0x0032,
			SHIVA = 0x0033,
			IBMAL = 0x0034,
			LOCK = 0x0035,
			TERMSRV = 0x0036,
			SRT = 0x0037,
			QUINCY = 0x0038,
			OPENAFS = 0x0039,
			AVID1 = 0x003A,
			DFS = 0x003B,
			KWNP = 0x003C,
			ZENWORKS = 0x003D,
			DRIVEONWEB = 0x003E,
			VMWARE = 0x003F,
			RSFX = 0x0040,
			MFILES = 0x0041,
			MS_NFS = 0x0042,
			GOOGLE = 0x0043,
			WNNC_CRED_MANAGER = 0xFFFF
		}
		public enum NetworkStatus
		{
			Running = 0, //NO_ERROR
			None = 1222, //ERROR_NO_NETWORK
			Busy = 170 //ERROR_BUSY
		}
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct NETINFOSTRUCT
		{
			public int cbStructureSize;
			public int dwProviderVersion;
			public NetworkStatus dwStatus;
			public int dwCharacteristics;
			//public int dwHandle; //ULONG_PTR
			public IntPtr dwHandle;
			public NetworkType wNetType;
			/// <summary>
			/// Set of bit flags indicating the valid print numbers for redirecting local printer devices,
			/// with the low-order bit corresponding to LPT1.
			/// </summary>
			public int dwPrinters;
			/// <summary>
			/// Set of bit flags indicating the valid local disk devices
			/// for redirecting disk drives, with the low-order bit corresponding to A:.
			/// </summary>
			public int dwDrives;

			public static NETINFOSTRUCT Prepare()
			{
				NETINFOSTRUCT ret = new NETINFOSTRUCT();
				ret.cbStructureSize = Marshal.SizeOf(typeof(NETINFOSTRUCT));
				return ret;
			}
		}
		//The WNetGetNetworkInformation function returns extended information about a specific network provider whose name 
		//was returned by a previous network enumeration.
		[DllImport("mpr.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern int WNetGetNetworkInformation
			([MarshalAs(UnmanagedType.LPTStr)]
			string lpProvider,
			ref  NETINFOSTRUCT lpNetInfoStruct);

		[DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
		private unsafe static extern int FormatMessage(int dwFlags, ref IntPtr lpSource,
			int dwMessageId, int dwLanguageId, ref String lpBuffer, int nSize, IntPtr* Arguments);


		#endregion
		public static bool Connect(string sServerPath)
		{
			NetworkCredential credentials;
			if (!sServerPath.StartsWith("\\\\"))
				return false;
			string sServerName = FileUtil.GetServerName(sServerPath);
            AlbumDB albumDB = new AlbumDB();
            albumDB.OpenConnection();
			credentials = albumDB.GetCredentials(sServerName); // try to load credentials from db
            albumDB.CloseConnection();
            albumDB.Dispose();
			if (credentials == null) // Prompt for login
			{
				using (LoginDialog ld = new LoginDialog(FileUtil.GetServerPath(sServerPath)))
				{
                    var dResult = ld.ShowDialog();
                    if (ld.Connected)
                    {
                        ld.Close();
                        return true;
                    }
                    else
                    {
                        ld.Close();
                        return false;
                    }
				}
			}
			Error result = AddConnection(sServerPath, credentials.UserName, credentials.Password);
			if (result.num != NO_ERROR)
			{
                MessageBox.Show(new Form() { TopMost = true }, result.message, "Connection failure", MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }
            return true;
		}
		public static Error AddConnection(string sRemoteName, string sUserName, string sPassword)
		{//makes a connection to a network resource and can redirect a local device to the network resource.
			if (!sRemoteName.StartsWith("\\\\"))
                return new Error(-1, "Internal error: Bad remote path '" + sRemoteName + "'");
            if (sRemoteName.EndsWith(@"\")) { sRemoteName = sRemoteName.Remove(sRemoteName.Length - 1, 1); }

			var netResource = new NetResource()
			{
				Scope = ResourceScope.GlobalNetwork,
				ResourceType = ResourceType.Disk,
				DisplayType = ResourceDisplaytype.Share,
				RemoteName = sRemoteName
			};
			var retVal = WNetAddConnection2(netResource, sPassword, sUserName, 0);

            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
			string err = GetErrorMessage(retVal);
			return new Error(retVal, err);
		}
		public static void CancelConnection(string sRemoteName)
		{
			if (!IsUNCPath(sRemoteName))
				return;
			if (sRemoteName.EndsWith(@"\"))
				sRemoteName = sRemoteName.Remove(sRemoteName.Length - 1, 1); 
			WNetCancelConnection2(sRemoteName, 0, true);
		}
		public static bool ResourceExists(string sRemoteName)
        {
            if (!sRemoteName.StartsWith("\\\\"))
                return false;
            if (sRemoteName.EndsWith(@"\")) { sRemoteName = sRemoteName.Remove(sRemoteName.Length - 1, 1); }

            var netResource = new NetResource()
            {
                Scope = ResourceScope.GlobalNetwork,
                ResourceType = ResourceType.Disk,
                DisplayType = ResourceDisplaytype.Share,
                RemoteName = sRemoteName
            };
            int ErrInfo = WNetUseConnection(IntPtr.Zero, ref netResource, null, null, 0, null, null, null);
			if (ErrInfo == NO_ERROR)
				return true;
			else
				return false;
		}
		public static string connectToRemote(string sRemoteName, string username, string password)
		{
			return connectToRemote(sRemoteName, username, password, false);
		}
		public static string connectToRemote(string remoteUNC, string username, string password, bool promptUser)
		{
			NetResource nr = new NetResource();
			nr.ResourceType = ResourceType.Disk;
			nr.RemoteName = remoteUNC;
			//			nr.lpLocalName = "F:";

			int ret;
			if (promptUser)
				ret = WNetUseConnection(IntPtr.Zero, nr, "", "", CONNECT_INTERACTIVE | CONNECT_PROMPT, null, null, null);
			else
				ret = WNetUseConnection(IntPtr.Zero, nr, password, username, 0, null, null, null);

			if (ret == NO_ERROR) return null;
			return GetErrorMessage(ret);
		}

		public static string disconnectRemote(string remoteUNC)
		{
			int ret = WNetCancelConnection2(remoteUNC, CONNECT_UPDATE_PROFILE, false);
			if (ret == NO_ERROR) return null;
			return GetErrorMessage(ret);
		}
		// GetErrorMessage formats and returns an error message
		// corresponding to the input errorCode.
		public unsafe static string GetErrorMessage(int errorCode)
		{
			int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
			int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
			int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;

			int messageSize = 255;
			String lpMsgBuf = "";
			int dwFlags = FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS;

			IntPtr ptrlpSource = IntPtr.Zero;
			IntPtr prtArguments = IntPtr.Zero;

			int retVal = FormatMessage(dwFlags, ref ptrlpSource, errorCode, 0, ref lpMsgBuf, messageSize, &prtArguments);
			if (retVal == 0)
				throw new Exception("Failed to format message for error code " + errorCode + ". ");

			return lpMsgBuf;
		}
		private static bool IsUNCPath(string path)
		{
			return path.StartsWith("\\") && path.IndexOf(@"\", 2) > -1;
		}
	}
}
