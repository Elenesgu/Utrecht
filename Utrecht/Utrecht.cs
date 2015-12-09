using System;
using System.Collections.Generic;
using System.Text;

namespace Utrecht {
	using WlanInterface = NativeWifi.WlanClient.WlanInterface;
	using Wlan = NativeWifi.Wlan;
	using System.Net.NetworkInformation;
	using System.Net;
	using System.IO;

	class Utrecht {

		static IWriter Writer;
		static NativeWifi.WlanClient Client;

		readonly static string ProfileNotExistString = "프로필이 존재하지 않음";

		static void Main (string[] args) {
			StartProcess();
		}

		private static void StartProcess () {
			int InterfaceNum = Client.Interfaces.GetLength(0);
			Writer.WriteLine("인터페이스의 수: {0}", InterfaceNum);

			int index = 0;
			foreach (var WlanInterface in Client.Interfaces) {
				Writer.WriteLine("  인터페이스 번호:\t {0}", index++);
				ShowInterface(WlanInterface);
			}

			WlanInterface SelectedInterface = null;
			if (InterfaceNum == 1) {
				Writer.WriteLine("\n인터페이스가 1개이므로, 해당 인터페이스를 사용합니다.");
				SelectedInterface = Client.Interfaces[0];
			} else if (InterfaceNum < 1) {
				Writer.WriteLine("\n인터페이스가 존재하지 않습니다.");
				return;
			} else {
				while (SelectedInterface != null) {
					Writer.Write("사용할 인터페이스 번호를 입력해 주세요: ");
					var InputString = Console.ReadLine();
					try {
						SelectedInterface = Client.Interfaces[Convert.ToInt32(InputString)];
					}
					catch (IndexOutOfRangeException) {
						SelectedInterface = null;
					}
				}
			}
			try {
				ProcessInterface(SelectedInterface);
			}
			catch (Exception e) {
				Writer.WriteLine(e.ToString());
				return;
			}
		}

		static void ProcessInterface (WlanInterface WlanInterface) {
			Wlan.WlanAvailableNetwork[] NetworkList = WlanInterface.GetAvailableNetworkList(0);
			Wlan.WlanAvailableNetwork? SelectedNetwork = null;
			while (true) {
				Writer.WriteLine();
				Writer.Write("조사할 SSID의 번호를 입력하세요.\n[q: 종료, a: 모든 SSID 탐색, s: SSID 목록 표시, o: 설정 변경, r: 재탐색]: ");
				var InputString = Console.ReadLine();
				if (InputString == "q") {
					return;
				} else if (InputString == "a") {
					foreach (var network in NetworkList) {
						ProcessNetwork(WlanInterface, network, true);
					}
				} else if (InputString == "s") {
					Writer.WriteLine();
					Writer.WriteLine("발견된 네트워크의 수: {0}", NetworkList.Length);
					Writer.WriteLine();
					int index = 0;
					foreach (var network in NetworkList) {
						Writer.WriteLine("[{0}]", index++);
						ShowNetwork(network);
						Writer.WriteLine();
					}
				} else if (InputString == "o") {
					Writer.Write("변경할 옵션을 선택하세요.\n[1: FTP 서버 주소, 2: FTP 자격증명,3: tracert 서버 주소]: ");
					InputString = Console.ReadLine();
					switch (InputString) {
						case "1":
							Writer.Write("변경할 주소를 입력하세요. [현재: {0}]\n> ", FTPServerHostname);
							InputString = Console.ReadLine();
							FTPServerHostname = InputString;
							Writer.WriteLine("{0}로 주소가 변경되었습니다.", FTPServerHostname);
							break;
						case "2":
							Writer.Write("ID를 입력하세요. [현재: {0}]: ", FTPServerUsername);
							InputString = Console.ReadLine();
							FTPServerUsername = InputString;
							Writer.Write("ID({0})에대한 비밀번호를 입력하세요. [암호화하여 저장하지 않습니다.]: ", FTPServerUsername);
							InputString = Console.ReadLine();
							FTPServerPassword = InputString;
							Writer.WriteLine("변경이 완료되었습니다.");
							break;
						case "3":
							Writer.Write("변경할 주소를 입력하세요. [현재: {0}]\n> ", ServerHostname);
							InputString = Console.ReadLine();
							ServerHostname = InputString;
							Writer.WriteLine("{0}로 주소가 변경되었습니다.", ServerHostname);
							break;
						default:
							Writer.WriteLine("잘못된 입력");
							break;
					}

				} else if (InputString == "r") {
					NetworkList = WlanInterface.GetAvailableNetworkList(0);
					Writer.WriteLine("네트워크 리스트를 갱신하였습니다.");
				} else {
					try {
						int Index = Convert.ToInt32(InputString);
						SelectedNetwork = NetworkList[Index];
						if (SelectedNetwork.HasValue) {
							ProcessNetwork(WlanInterface, SelectedNetwork.Value);
						} else {
							throw new NullReferenceException();
						}
					}
					catch (Exception e) when (e is IndexOutOfRangeException || e is NullReferenceException ||
					e is FormatException) {
						Writer.WriteLine("잘못된 입력입니다.");
					}
				}
			}
		}

		static void ProcessNetwork (WlanInterface WlanInterface, Wlan.WlanAvailableNetwork Network, bool isAll = false) {
			string profile = GetSSID(Network);
			try {
				string profileXML = WlanInterface.GetProfileXml(Network.profileName);
			}
			catch(Exception) {
				Writer.WriteLine("[SSID: {0}]프로필 없는 네트워크 포인트 입니다.", profile);
				return;
			}

			Writer.WriteLine();
			Writer.WriteLine("네트워크 정보");
			ShowNetwork(Network);
			Writer.WriteLine();
			while (true) {
				string InputString;
				if (isAll == false) {
					Writer.WriteLine("무엇을 하시겠습니까?");
					Writer.Write("[1: 테스트, 2: 대역폭만, 3: RTT만, q: 종료]: ");
					InputString = Console.ReadLine();
				}
				else {
					InputString = "1";
				}
				
				Wlan.WlanBssEntry[] BSSIDList;
				BSSIDList = Client.Interfaces[0].GetNetworkBssList(Network.dot11Ssid, Network.dot11BssType, true);
				int BSSIDIndex = 0;
                    switch (InputString) {
					case "1": case "2": case "3":
						foreach (var BSSID in BSSIDList) {
							string MacAddress = BitConverter.ToString(BSSID.dot11Bssid).Replace("-", ":");
							int BSSIDSignalQuality = (int) BSSID.linkQuality;
							int RSSI = BSSID.rssi;
							int Channel = GetChannel(BSSID.chCenterFrequency);
							WlanInterface.ConnectSynchronously(NativeWifi.Wlan.WlanConnectionMode.Profile, NativeWifi.Wlan.Dot11BssType.Infrastructure, profile, 5000, MacAddress);
							Writer.WriteLine("[{0}]", BSSIDIndex++);
							Writer.WriteLine("  MAC 주소:\t {0}", MacAddress);
							Writer.WriteLine("  신호세기:\t {0} (RSSI: {1} dBm)", BSSIDSignalQuality, RSSI);

							if (Channel < 1 || Channel > 12) {
								Channel = GetChannel5G(BSSID.chCenterFrequency);
								Writer.WriteLine("  채널:\t {0}Ch(5Ghz)", Channel);
							} else {
								Writer.WriteLine("  채널:\t {0}Ch", Channel);
							}

							if (InputString == "1" || InputString == "2") {
								double speed = MeasureNetworkSpeed(Network);
								if(speed > 1024) {
									Writer.WriteLine("  대역폭:\t {0}MB/s", (speed/1024).ToString("F3"));
								}
								else {
									Writer.WriteLine("  대역폭:\t {0}KB/s", speed.ToString("F3"));
								}
							}
							if (InputString == "1" || InputString == "3") {
								pingCounter = 0;
								var result = MeasureNetworkStatus(Network);
								if (result != null) {
									Writer.WriteLine("  {0}번째 라우터({1}) RTT:\t {2}ms", result.Item1.TTL, result.Item1.router, result.Item1.RTT);
									Writer.WriteLine("  {0}번쨰 라우터({1}) RTT:\t {2}ms", result.Item2.TTL, result.Item2.router, result.Item2.RTT);
								}
							}
						}
						break;
					case "q":
						return;
					default:
						break;
				}
			}
		}

		static string ServerHostname = "elenesgu.asuscomm.com";
		static string FTPServerHostname = "ftp://elenesgu.asuscomm.com/test/10MB.txt";
		static string FTPServerUsername = "anonymous";
		static string FTPServerPassword = "";
		static double MeasureNetworkSpeed (Wlan.WlanAvailableNetwork Network) {
			int fileSize = 100 << 20;
			try {
				FtpWebRequest req = WebRequest.Create(FTPServerHostname) as FtpWebRequest;
				req.Credentials = new NetworkCredential(FTPServerUsername, FTPServerPassword);
				req.Method = WebRequestMethods.Ftp.GetFileSize;
				using (FtpWebResponse resp = (FtpWebResponse) req.GetResponse()) {
					fileSize = (int)resp.ContentLength;
				}

				req = WebRequest.Create(FTPServerHostname) as FtpWebRequest;
				req.Credentials = new NetworkCredential(FTPServerUsername, FTPServerPassword);
				req.Method = WebRequestMethods.Ftp.DownloadFile;
				var stopWatch = new System.Diagnostics.Stopwatch();
				stopWatch.Start();
				using (FtpWebResponse resp = (FtpWebResponse) req.GetResponse()) {
					Stream stream = resp.GetResponseStream();
					using (StreamReader reader = new StreamReader(stream)) {
						string data = reader.ReadToEnd();
					}
				}
				stopWatch.Stop();
				long timeTaken = stopWatch.ElapsedMilliseconds;
				double speed = fileSize / ((double) timeTaken / 1000.0);
				return speed / 1024.0;
			}
			catch (WebException) {
				Writer.WriteLine("FTP전송에서 오류가 발생했습니다.");
				return 0.0;
			}
		}
		public struct PingResult {
			public IPAddress router;
			public long RTT;
			public long TTL;
		}

		static readonly string PingData = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
		static int pingCounter = 0;
		static Tuple<PingResult, PingResult> MeasureNetworkStatus (Wlan.WlanAvailableNetwork Network) {
			try {
				var pingSender = new Ping();
				var pingOption = new PingOptions(1, true);
				var trace = GetTraceRoute(ServerHostname) as List<PingResult>;
				PingResult first, last;
				first = new PingResult();
				last = new PingResult();
				long max = 0;
				foreach(var ping in trace) {
					if(ping.TTL == 1) {
						first = ping;
					}
					if(max < ping.TTL) {
						max = ping.TTL;
						last = ping;
					}
				}
				return Tuple.Create(first, last);
			}
			catch (PingException) {
				if (pingCounter < 4) {
					Console.WriteLine("Ping 전달 중 이상이 발생했습니다. 재시도... {0}/4", (pingCounter++) + 1);
					return MeasureNetworkStatus(Network);
				} else {
					return null;
				}
			}
		}

		static IEnumerable<PingResult> GetTraceRoute(string hostName) {
			return GetTraceRoute(hostName, 1);
		}

		static IEnumerable<PingResult> GetTraceRoute (string hostName, int ttl) {
			var pingSender = new Ping();
			var pingOption = new PingOptions(ttl, true);
			const int timeout = 1000;
			byte[] buffer = Encoding.ASCII.GetBytes(PingData);
			var reply = default(PingReply);

			reply = pingSender.Send(hostName, timeout, buffer, pingOption);

			var result = new List<PingResult>();
			if(reply.Status == IPStatus.Success) {
				result.Add(new PingResult() {
					router = reply.Address,
					RTT = reply.RoundtripTime,
					TTL = ttl
				});
			}
			else if (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.TimedOut) {
				if(reply.Status == IPStatus.TtlExpired) {
					result.Add(new PingResult() {
						router = reply.Address,
						RTT = reply.RoundtripTime,
						TTL = ttl
					});
				}
				IEnumerable<PingResult> tempResult = default(IEnumerable<PingResult>);
				tempResult = GetTraceRoute(hostName, ttl + 1);
				result.AddRange(tempResult);
			}
			else {
				Writer.WriteLine("RTT 측정에 실패했습니다. 호스트 주소: {0}", hostName);
			}
			return result;
		}

		static void ShowInterface(WlanInterface WlanInterface) {
			Writer.WriteLine("  인터페이스 이름:\t {0}", WlanInterface.InterfaceDescription);
			Writer.WriteLine("  인터페이스 상태:\t {0}", WlanInterface.InterfaceState);
		}

		static void ShowNetwork(Wlan.WlanAvailableNetwork Network) {
			string SSID = GetSSID(Network);
			int SignalQuality = (int) Network.wlanSignalQuality;
			bool IsEncrypted = Network.securityEnabled;
			string AuthorMethod = Network.dot11DefaultAuthAlgorithm.ToString();
			string EcrypMethod = Network.dot11DefaultCipherAlgorithm.ToString();
			string ProfileName = Network.profileName;
			if (ProfileName.Length == 0) {
				ProfileName = ProfileNotExistString;
			}

			var BSSIDList = Client.Interfaces[0].GetNetworkBssList(Network.dot11Ssid, Network.dot11BssType,
				true);
			Writer.WriteLine("  SSID:\t\t {0}", SSID);

			Writer.WriteLine("    BSSID 수:\t {0}", BSSIDList.Length);
			int BSSIDIndex = 0;
			foreach (var BSSID in BSSIDList) {
				string MacAddress = BitConverter.ToString(BSSID.dot11Bssid).Replace("-", ":");
				int BSSIDSignalQuality = (int) BSSID.linkQuality;
				int RSSI = BSSID.rssi;
				var RateSet = BSSID.wlanRateSet;
				int Channel = GetChannel(BSSID.chCenterFrequency);
				

				Writer.WriteLine("      [{0}]", BSSIDIndex++);
				Writer.WriteLine("        MAC 주소:\t {0}", MacAddress);
				Writer.WriteLine("        신호세기:\t {0} (RSSI: {1} dBm)", BSSIDSignalQuality, RSSI);
				if (Channel < 1 || Channel > 12) {
					Channel = GetChannel5G(BSSID.chCenterFrequency);
					Writer.WriteLine("        채널:\t {0}Ch(5Ghz)", Channel);
				} else {
					Writer.WriteLine("        채널:\t {0}Ch", Channel);
				}
			}

			Writer.WriteLine("  신호세기:\t {0}", SignalQuality);
			Writer.WriteLine("  암호화:\t {0}", (IsEncrypted) ? "암호화 됨" : "암호화 되지 않음");
			Writer.WriteLine("  인증 방법:\t {0}", AuthorMethod);
			Writer.WriteLine("  암호 방법:\t {0}", EcrypMethod);
			Writer.WriteLine("  프로필 이름:\t {0}", ProfileName);
		}

		private static string GetSSID (Wlan.WlanAvailableNetwork Network) {
			string SSID = Encoding.ASCII.GetString(Network.dot11Ssid.SSID,
							0, (int) Network.dot11Ssid.SSIDLength);
			if (SSID.Contains("\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0")) {
				SSID = "숨겨진 네트워크";
			}

			return SSID;
		}

		private static int GetChannel (uint chCenterFrequency) {
			uint tmp = (chCenterFrequency % 2412000) / 1000;
			uint tmp2 = tmp / 5;
			int result = (int) tmp + 1;
			return result;
		}

		private static int GetChannel5G (uint chCenterFrequency) {
			uint tmp = (chCenterFrequency % 5000) / 1000;
			uint tmp2 = tmp / 5;
			int result = (int) tmp + 1;
			return result;
		}

		static Utrecht() {
			Writer = new ConsoleWriter();
			Client = new NativeWifi.WlanClient();
		}
	}
}
