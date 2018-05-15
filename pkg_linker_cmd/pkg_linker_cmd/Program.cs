using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.IO;
using System.Data;
using System.Media;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace pkg_linker_cmd
{
	class Program
	{
		byte[] Name2 = getByteArray("504152414d2e53464f");
		//PSP
		private byte[] PSPAesKey = new byte[16] {
			0x07, 0xF2, 0xC6, 0x82, 0x90, 0xB5, 0x0D, 0x2C,
			0x33, 0x81, 0x8D, 0x70, 0x9B, 0x60, 0xE6, 0x2B
		};

		//PS3
		private byte[] PS3AesKey = new byte[16] {
			0x2E, 0x7B, 0x71, 0xD7, 0xC9, 0xC9, 0xA1, 0x4E,
			0xA3, 0x22, 0x1F, 0x18, 0x88, 0x28, 0xB8, 0xF8
		};

		byte[] AesKey = new byte[16];

		byte[] PKGFileKey = new byte[16];

		byte[] PKGXorKey = new byte[16];

		uint uiEncryptedFileStartOffset = 0;

		uint uiNumberofFiles = 0;
		public string ip;
		public string port = "8486";
		public string serverport = "8486";
		public string settingsport = "8486";
		FileInfo[] Files;
		DirectoryInfo dinfo;
		int count = 1001;
		int i = 0;
		int o = 1;
		int pl;
		string pkg;
		static string t = null;
		static string s = null;
		static string scid = null;

		static byte[] key = new byte[0x40];
		static uint size = 0;
		static uint start = 0;
		static byte[] fk = new byte[0x08];
		static byte[] ek = new byte[0x08];

		static byte[] temp = new byte[0x08];
		static uint offset = 0;


		string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;//System.Windows.Shapes.Path.GetDirectoryName(Application.ExecutablePath.);
		static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };


		static void Main(string[] args)
		{
			Program p = new Program();
			p.On_start();

		}

		public void On_start()
		{
			pl = (int)Environment.OSVersion.Platform;
			if ((pl == 4) || (pl == 6) || (pl == 128))
			{
				//Console.WriteLine("Running on Unix");
			}
			else
			{
				//Console.WriteLine("NOT running on Unix");
			}
			//read_header("jjj_-_Copy_(3).pkg");
			//extractIcons2();
			O_settings();
			O_folder();
			S_xml("hdd");
			//s_xml("usb");
			Clean();
		}

		#region<<worker>>

		private void O_settings()
		{
			if (File.Exists("settings.txt"))
			{
				foreach (string line in File.ReadLines("settings.txt"))
				{
					if (line.Contains("IP="))
					{
						ip = line.Replace("IP=", "");
					}

					if (line.Contains("PORT="))
					{
						port = line.Replace("PORT=", "");
					}

					//Console.WriteLine("-- {0}", line);
				}
			}

			else
			{
				Console.WriteLine("settings.txt not found");
			}
		}

		static string SizeSuffix(Int64 value, int decimalPlaces = 1)
		{
			if (value < 0) { return "-" + SizeSuffix(-value); }

			int c = 0;
			decimal dValue = (decimal)value;
			while (Math.Round(dValue, decimalPlaces) >= 1000)
			{
				dValue /= 1024;
				c++;
			}

			return string.Format("{0:n" + decimalPlaces + "} {1}", dValue, SizeSuffixes[c]);
		}

		public void Clean()
		{
			if (File.Exists("ICON0.PNG"))
			{
				File.Delete("ICON0.PNG");
			}

			DirectoryInfo dinfo2 = new DirectoryInfo(appPath);
			//files = Directory.GetFiles(appPath, "*.pkg");
			FileInfo[] Files2 = dinfo2.GetFiles("*.Dec");
			string[] tempsubdirectoryEntries = Directory.GetDirectories(appPath);


			foreach (FileInfo file in Files2)
			{
				File.Delete(file.FullName);
			}


		}
		#endregion<<worker>>

		#region<<files>>

		private void O_folder()
		{

			i = 0;
			//extractIcons();


			appPath = appPath.Replace("pkg_linker_cmd.exe", "");
			pkg = appPath;
			Rename();
			ExtractIcons();
			dinfo = new DirectoryInfo(appPath);
			//files = Directory.GetFiles(appPath, "*.pkg");
			Files = dinfo.GetFiles("*.pkg");
			string[] tempsubdirectoryEntries = Directory.GetDirectories(appPath);


			foreach (FileInfo file in Files)
			{
				FileStream pkgFilehead = File.Open(file.FullName, FileMode.Open);
				byte[] testmagic = new byte[0x06];
				//pkgFilehead.Seek(0x30, SeekOrigin.Begin);
				pkgFilehead.Read(testmagic, 0, 0x06);
				pkgFilehead.Close();
				byte[] magic1 = new byte[] { 0x7F, 0x50, 0x4B, 0x47, 0x00, 0x00 };
				byte[] magic2 = new byte[] { 0x7F, 0x50, 0x4B, 0x47, 0x80, 0x00 };
				string pkgtype;
				bool isdebug = testmagic.SequenceEqual(magic1);
				bool isretail = testmagic.SequenceEqual(magic2);
				if (isdebug == true)
				{
					pkgtype = "Debug ";
				}

				else if (isretail == true)
				{
					pkgtype = "Retail";
				}

				else
				{
					pkgtype = "";

				}

				if (pkgtype == "Retail")
				{
					byte[] data = new byte[0x80];
					byte[] result;
					byte[] testsha = new byte[0x08];


					FileStream pkgFilesha = File.Open(file.FullName, FileMode.Open);
					byte[] readsha = new byte[0x08];
					//pkgFilehead.Seek(0x30, SeekOrigin.Begin);
					pkgFilesha.Read(data, 0, 0x80);
					pkgFilesha.Seek(0xb8, SeekOrigin.Begin);
					pkgFilesha.Read(readsha, 0, 0x08);
					pkgFilesha.Close();


					SHA1 sha = new SHA1CryptoServiceProvider();
					// This is one implementation of the abstract class SHA1.
					result = sha.ComputeHash(data);
					Buffer.BlockCopy(result, 12, testsha, 0, 8);

					bool ishan = !readsha.SequenceEqual(testsha);
					if (ishan == true)
					{
						pkgtype = "HAN   ";
					}
				}

				if (pkgtype != "")
				{
					s = file.ToString();
					if (s != "Package_List.pkg")
					{
						s = s.Replace("&", "and");

						FileStream pkgFile = File.Open(file.FullName, FileMode.Open);
						byte[] cid = new byte[0x30];
						pkgFile.Seek(0x30, SeekOrigin.Begin);
						pkgFile.Read(cid, 0, 0x30);
						pkgFile.Close();
						scid = Encoding.ASCII.GetString(cid);
						int index = scid.IndexOf("\0");
						if (index > 0)
							scid = scid.Substring(0, index);
						string sz = SizeSuffix(file.Length);
						s = file.ToString();


						string iconpath = s.Replace(".pkg", ".PNG");
						iconpath = appPath + "bin/icons/" + iconpath;
						if (!File.Exists(iconpath))
						{
							iconpath = appPath + "bin/icons/download.png";
						}

						i++;
					}

				}

			}

		}

		private void Patch_local_xml()
		{
			if (File.Exists("bin/local_ps3xploit.templet"))
			{
				using (StreamReader sr = new StreamReader("bin/local_ps3xploit.templet"))
				{
					string ip_port = ip;
					if (port != "80")
					{
						ip_port = ip + ":" + port;
					}

					string findpm1 = "patch_ip";
					string contents = sr.ReadToEnd();
					sr.Close();
					if (contents.Contains(findpm1))
					{

						contents = contents.Replace(findpm1, ip_port);
						if (File.Exists("bin/PS3_GAME/USRDIR/local_ps3xploit.xml"))
						{
							File.Delete("bin/PS3_GAME/USRDIR/local_ps3xploit.xml");
						}
						File.Copy("bin/local_ps3xploit.templet", "bin/PS3_GAME/USRDIR/local_ps3xploit.xml");
						File.WriteAllText("bin/PS3_GAME/USRDIR/local_ps3xploit.xml", contents);
						//File.Copy("bin/local_ps3xploit.xml", "bin/PS3_GAME/USRDIR/local_ps3xploit.xml");
						//File.Move("bin/local_ps3xploit.xml", "bin/local_ps3xploit.xml");


					}



				}

			}

		}

		private void Patch_server_setup_xml()
		{
			if (File.Exists("bin/server_setup.templet"))
			{
				using (StreamReader sr = new StreamReader("bin/server_setup.templet"))
				{
					string ip_port = ip;
					if (port != "80")
					{
						ip_port = ip + ":" + port;
					}

					string findpm1 = "patch_ip_port";
					string findpm2 = "patch_ip";
					string findpm3 = "patch_port";
					string findpm4 = "patch_folder";
					string contents = sr.ReadToEnd();
					sr.Close();
					if (contents.Contains(findpm1))
					{

						contents = contents.Replace(findpm1, ip_port);
						contents = contents.Replace(findpm2, ip);
						contents = contents.Replace(findpm3, port);
						contents = contents.Replace(findpm4, appPath);
						if (File.Exists("bin/PS3_GAME/USRDIR/server_setup.xml"))
						{
							File.Delete("bin/PS3_GAME/USRDIR/server_setup.xml");
						}
						File.Copy("bin/server_setup.templet", "bin/PS3_GAME/USRDIR/server_setup.xml");
						File.WriteAllText("bin/PS3_GAME/USRDIR/server_setup.xml", contents);
						//File.Copy("bin/local_ps3xploit.xml", "bin/PS3_GAME/USRDIR/local_ps3xploit.xml");
						//File.Move("bin/local_ps3xploit.xml", "bin/local_ps3xploit.xml");


					}



				}

			}

		}

		private void S_xml(string xtype)
		{
			string ip_port = ip;
			if (port != "80")
			{
				ip_port = ip + ":" + port;
			}

			string ipd = "";
			string ics = "";
			count = 1002;
			if (xtype == "usb")
			{
				ipd = "http://" + ip_port + "/bin/icons/";
				ics = pkg + " on " + ip_port;
			}
			if (xtype == "hdd")
			{
				ipd = "/dev_hdd0/game/PKGLINKER/USRDIR/icons/";
				ics = "/dev_hdd0/game/PKGLINKER/USRDIR/";
			}
			using (System.IO.StreamWriter nfile =
			new System.IO.StreamWriter(@"bin/package_link.xml", false))
			{

				nfile.WriteLine("<?xml version=" + '"' + "1.0" + '"' + " encoding=" + '"' + "UTF-8" + '"' + "?> \n \n<XMBML version=" + '"' + "1.0" + '"' + ">");
				if (xtype == "usb")
				{
					nfile.WriteLine("	<View id=" + '"' + "package_link" + '"' + "> \n		<Attributes> \n			<Table key=" + '"' + "pkg_main" + '"' + ">");
				}
				else if (xtype == "hdd")
				{
					nfile.WriteLine("	<View id=" + '"' + "package_link_game" + '"' + "> \n		<Attributes> \n			<Table key=" + '"' + "pkg_main" + '"' + ">");
				}
				nfile.WriteLine("				<Pair key=" + '"' + "icon_rsc" + '"' + "><String>tex_album_icon</String></Pair>");
				nfile.WriteLine("				<Pair key=\"icon_notation\"><String>WNT_XmbItemAlbum</String></Pair>");
				nfile.WriteLine("				<Pair key=" + '"' + "title" + '"' + "><String>? Install Packages From Webserver</String></Pair>");
				nfile.WriteLine("				<Pair key=" + '"' + "info" + '"' + "><String>" + pkg + " on " + ip_port + "</String></Pair>");
				nfile.WriteLine("				<Pair key=" + '"' + "ingame" + '"' + "><String>disable</String></Pair>\n			</Table>\n		</Attributes>\n			<Items>\n			<Query ");
				nfile.WriteLine("				class=" + '"' + "type:x-xmb/folder-pixmap" + '"');
				nfile.WriteLine("				key=" + '"' + "pkg_main" + '"');
				nfile.WriteLine("				attr=" + '"' + "pkg_main" + '"');
				nfile.WriteLine("				src=" + '"' + "#pkg_items" + '"' + "/>");
				nfile.WriteLine("			</Items>\n		</View>\n		<View id=" + '"' + "pkg_items" + '"' + ">	\n		<Attributes>");
				i = 0;

				string pkgt1 = "			<Table key=\"server_setup\">\n				<Pair key=\"icon\"><String>http://" + ip_port + "/bin/icons/setup.png</String></Pair>\n				<Pair key=\"title\"><String>Server Setup</String></Pair>\n 				<Pair key=\"info\"><String>Update List, Rescan and Other Server Controls</String></Pair>\n 			</Table>";
				string pkgt2 = "			<Table key=\"server_setup\">\n				<Pair key=\"icon\"><String>/dev_hdd0/game/PKGLINKER/USRDIR/icons/setup.png</String></Pair>\n				<Pair key=\"title\"><String>Server Setup</String></Pair>\n 				<Pair key=\"info\"><String>Update List, Rescan and Other Server Controls</String></Pair>\n 			</Table>";
				string pkgm = "			<Query\n				class=\"type:x-xmb/folder-pixmap\"\n				key=\"server_setup\"\n				attr=\"server_setup\"\n				src=\"xmb://localhost/dev_hdd0/game/PKGLINKER/USRDIR/server_setup.xml#server_setup_items\"/>";

				//string pkgb = "		<View id=\"pkg_000_item\">\n		<Attributes>\n			<Table key=\"link000\">\n				<Pair key=\"info\"><String>net_package_install</String></Pair>\n				<Pair key=\"pkg_src\"><String>http://" + ip_port + "/Package_List.pkg</String></Pair>\n				<Pair key=\"pkg_src_qa\"><String>http://" + ip_port + "/Package_List.pkg</String></Pair>\n				<Pair key=\"content_name\"><String>pkg_install_pc</String></Pair>\n				<Pair key=\"content_id\"><String>UP0100-PKGLINKER_00-PINK1000DEVIL303</String></Pair>\n				<Pair key=\"prod_pict_path\"><String>" + ipd + "Refresh_Package_List.PNG</String></Pair>\n			</Table>\n 		</Attributes>\n 			<Items>\n			<Item class=\"type:x-xmb/xmlnpsignup\" key=\"link000\" attr=\"link000\"/>\n		</Items>\n 		</View>";

				//string Server_ping1 = "			<Table key=\"pkg_001\">\n				<Pair key=\"icon\"><String>/dev_hdd0/game/PKGLINKER/USRDIR/icons/Server_Check.PNG</String></Pair>\n				<Pair key=\"title\"><String>Check Server Connection</String></Pair>\n				<Pair key=\"info\"><String>Ping Webserver @ " + ip_port + "</String></Pair>\n				<Pair key=\"module_name\"><String>webrender_plugin</String></Pair>\n				<Pair key=\"module_action\"><String>http://" + ip_port + "/bin/Server_Check.php</String></Pair>\n			</Table>";
				//string Server_ping2 = "			<Query\n				class=\"type:x-xmb/module-action\"\n				key=\"pkg_001\"\n				attr=\"pkg_001\"/>";


				if (xtype == "usb")
				{
					nfile.WriteLine(pkgt1); ;
				}
				else
				{
					nfile.WriteLine(pkgt2); ;
				}

				//nfile.WriteLine(Server_ping1);

				foreach (FileInfo file in Files)
				{
					FileStream pkgFilehead = File.Open(file.FullName, FileMode.Open);
					byte[] testmagic = new byte[0x06];
					pkgFilehead.Read(testmagic, 0, 0x06);
					pkgFilehead.Close();
					byte[] magic1 = new byte[] { 0x7F, 0x50, 0x4B, 0x47, 0x00, 0x00 };
					byte[] magic2 = new byte[] { 0x7F, 0x50, 0x4B, 0x47, 0x80, 0x00 };
					string pkgtype;
					bool isdebug = testmagic.SequenceEqual(magic1);
					bool isretail = testmagic.SequenceEqual(magic2);
					if (isdebug == true)
					{
						pkgtype = "Debug ";
					}

					else if (isretail == true)
					{
						pkgtype = "Retail";
					}

					else
					{
						pkgtype = "";

					}

					if (pkgtype == "Retail")
					{
						byte[] data = new byte[0x80];
						byte[] result;
						byte[] testsha = new byte[0x08];


						FileStream pkgFilesha = File.Open(file.FullName, FileMode.Open);
						byte[] readsha = new byte[0x08];
						//pkgFilehead.Seek(0x30, SeekOrigin.Begin);
						pkgFilesha.Read(data, 0, 0x80);
						pkgFilesha.Seek(0xb8, SeekOrigin.Begin);
						pkgFilesha.Read(readsha, 0, 0x08);
						pkgFilesha.Close();


						SHA1 sha = new SHA1CryptoServiceProvider();
						// This is one implementation of the abstract class SHA1.
						result = sha.ComputeHash(data);
						Buffer.BlockCopy(result, 12, testsha, 0, 8);

						bool ishan = !readsha.SequenceEqual(testsha);
						if (ishan == true)
						{
							pkgtype = "HAN";
						}
						else
						{
							pkgtype = "Retail";
						}
					}

					if (pkgtype != "")
					{
						if (pkgtype == "Debug ")
						{
							pkgtype = "Debug";
						}
						s = file.ToString();
						if (s != "Package_List.pkg")
						{

							t = Convert.ToString(count);
							t = t.Remove(0, 1);
							FileStream pkgFile = File.Open(file.FullName, FileMode.Open);
							byte[] cid = new byte[0x30];
							pkgFile.Seek(0x30, SeekOrigin.Begin);
							pkgFile.Read(cid, 0, 0x30);
							byte[] cids = new byte[0x9];
							pkgFile.Seek(0x37, SeekOrigin.Begin);
							pkgFile.Read(cids, 0, 0x9);
							pkgFile.Close();

							scid = Encoding.ASCII.GetString(cid);

							string scids = Encoding.ASCII.GetString(cids);

							int index = scid.IndexOf("\0");
							if (index > 0)
								scid = scid.Substring(0, index);



							string sz = SizeSuffix(file.Length);
							s = file.ToString();



							s = s.Replace("&", "and");
							string imf = s;
							imf = imf.Replace(".PKG", ".PNG");
							imf = imf.Replace(".pkg", ".PNG");
							imf = imf.Replace("&", "and");
							imf = imf.Replace(" ", "%20");

							string imj = s;
							imj = imj.Replace(".PKG", ".JPG");
							imj = imj.Replace(".pkg", ".JPG");
							imj = imj.Replace("&", "and");
							imj = imj.Replace(" ", "%20");

							string imn = s;
							imn = imn.Replace(".PKG", ".PNG");
							imn = imn.Replace(".pkg", ".PNG");
							imn = imn.Replace("&", "and");
							imn = imn.Replace(" ", "%20");

							string imnj = s;
							imnj = imnj.Replace(".PKG", ".JPG");
							imnj = imnj.Replace(".pkg", ".JPG");
							imnj = imnj.Replace("&", "and");
							imnj = imnj.Replace(" ", "%20");

							s = s.Replace(".pkg", "");
							s = s.Replace(".PKG", "");
							s = s.Replace("_", " ");

							nfile.WriteLine("			<Table key=" + '"' + "pkg_" + t + '"' + ">");
							if (File.Exists("bin/icons/" + imn))
							{
								nfile.WriteLine("				<Pair key=" + '"' + "icon" + '"' + "><String>" + ipd + imn + "</String></Pair>");
							}
							else if (File.Exists("bin/icons/" + imnj))
							{
								nfile.WriteLine("				<Pair key=" + '"' + "icon" + '"' + "><String>" + ipd + imnj + "</String></Pair>");
							}
							else
							{
								nfile.WriteLine("				<Pair key=" + '"' + "icon" + '"' + "><String>" + ipd + "download.png</String></Pair>");
							}
							nfile.WriteLine("				<Pair key=" + '"' + "title" + '"' + "><String>" + s + "</String></Pair>");
							nfile.WriteLine("				<Pair key=" + '"' + "info" + '"' + "><String>" + scids + "   [" + pkgtype + "]   " + sz + "</String></Pair>");
							nfile.WriteLine(" 			</Table>");

							count++;

							i++;
						}
					}
				}

				nfile.WriteLine("		</Attributes>\n			<Items>");
				nfile.WriteLine(pkgm);
				//nfile.WriteLine(Server_ping2);
				count = 1002;
				i = 0;
				foreach (FileInfo file in Files)
				{
					FileStream pkgFilehead = File.Open(file.FullName, FileMode.Open);
					byte[] testmagic = new byte[0x08];
					pkgFilehead.Read(testmagic, 0, 0x08);
					pkgFilehead.Close();
					byte[] magic1 = new byte[] { 0x7F, 0x50, 0x4B, 0x47, 0x00, 0x00, 0x00, 0x01 };
					byte[] magic2 = new byte[] { 0x7F, 0x50, 0x4B, 0x47, 0x80, 0x00, 0x00, 0x01 };
					string pkgtype;
					bool isdebug = testmagic.SequenceEqual(magic1);
					bool isretail = testmagic.SequenceEqual(magic2);
					if (isdebug == true)
					{
						pkgtype = "Debug ";
					}

					else if (isretail == true)
					{
						pkgtype = "Retail";
					}

					else
					{
						pkgtype = "";

					}

					if (pkgtype != "")
					{
						s = file.ToString();
						if (s != "Package_List.pkg")
						{
							t = Convert.ToString(count);
							t = t.Remove(0, 1);



							nfile.WriteLine("			<Query\n				class=" + '"' + "type:x-xmb/folder-pixmap" + '"');
							nfile.WriteLine("				key=" + '"' + "pkg_" + t + '"');
							nfile.WriteLine("				attr=" + '"' + "pkg_" + t + '"');
							nfile.WriteLine("				src=" + '"' + "#pkg_" + t + "_item" + '"' + "/>");

							count++;

							i++;
						}
					}
				}
				nfile.WriteLine("			</Items>\n		</View>\n");
				// nfile.WriteLine(pkgb);
				nfile.WriteLine("\n");
				count = 1002;
				i = 0;
				foreach (FileInfo file in Files)
				{
					FileStream pkgFilehead = File.Open(file.FullName, FileMode.Open);
					byte[] testmagic = new byte[0x08];
					pkgFilehead.Read(testmagic, 0, 0x08);
					pkgFilehead.Close();
					byte[] magic1 = new byte[] { 0x7F, 0x50, 0x4B, 0x47, 0x00, 0x00, 0x00, 0x01 };
					byte[] magic2 = new byte[] { 0x7F, 0x50, 0x4B, 0x47, 0x80, 0x00, 0x00, 0x01 };
					string pkgtype;
					bool isdebug = testmagic.SequenceEqual(magic1);
					bool isretail = testmagic.SequenceEqual(magic2);
					if (isdebug == true)
					{
						pkgtype = "Debug ";
					}

					else if (isretail == true)
					{
						pkgtype = "Retail";
					}

					else
					{
						pkgtype = "";

					}

					if (pkgtype != "")
					{
						s = file.ToString();
						if (s != "Package_List.pkg")
						{
							t = Convert.ToString(count);
							t = t.Remove(0, 1);
							FileStream pkgFile = File.Open(file.FullName, FileMode.Open);
							byte[] cid = new byte[0x30];
							pkgFile.Seek(0x30, SeekOrigin.Begin);
							pkgFile.Read(cid, 0, 0x30);
							pkgFile.Close();
							scid = Encoding.ASCII.GetString(cid);
							int index = scid.IndexOf("\0");
							if (index > 0)
								scid = scid.Substring(0, index);

							string imf = s;
							imf = imf.Replace(".PKG", ".PNG");
							imf = imf.Replace(".pkg", ".PNG");
							imf = imf.Replace("&", "and");
							imf = imf.Replace(" ", "%20");
							//imf = imf.Replace("_", "%20");

							string imj = s;
							imj = imj.Replace(".PKG", ".JPG");
							imj = imj.Replace(".pkg", ".JPG");
							imj = imj.Replace("&", "and");
							imj = imj.Replace(" ", "%20");

							string imn = s;
							imn = imn.Replace(".PKG", ".PNG");
							imn = imn.Replace(".pkg", ".PNG");
							imn = imn.Replace("&", "and");
							imn = imn.Replace(" ", "%20");
							string imnj = s;
							imnj = imnj.Replace(".PKG", ".JPG");
							imnj = imnj.Replace(".pkg", ".JPG");
							imnj = imnj.Replace("&", "and");
							imnj = imnj.Replace(" ", "%20");
							string wfile = file.ToString();
							wfile = wfile.Replace(" ", "%20");



							nfile.WriteLine("		<View id=" + '"' + "pkg_" + t + "_item" + '"' + ">");
							nfile.WriteLine("		<Attributes>");
							nfile.WriteLine("			<Table key=" + '"' + "link" + t + '"' + ">");
							nfile.WriteLine("				<Pair key=" + '"' + "info" + '"' + "><String>net_package_install</String></Pair>");
							nfile.WriteLine("				<Pair key=" + '"' + "pkg_src" + '"' + "><String>http://" + ip_port + "/" + file + "</String></Pair>");
							nfile.WriteLine("				<Pair key=" + '"' + "pkg_src_qa" + '"' + "><String>http://" + ip_port + "/" + file + "</String></Pair>");
							nfile.WriteLine("				<Pair key=" + '"' + "content_name" + '"' + "><String>pkg_install_pc</String></Pair>");
							nfile.WriteLine("				<Pair key=" + '"' + "content_id" + '"' + "><String>" + scid + "</String></Pair>");
							if (File.Exists("bin/icons/" + imn))
							{
								nfile.WriteLine("				<Pair key=" + '"' + "prod_pict_path" + '"' + "><String>" + ipd + imn + "</String></Pair>");
							}
							else if (File.Exists("bin/icons/" + imnj))
							{
								nfile.WriteLine("				<Pair key=" + '"' + "prod_pict_path" + '"' + "><String>" + ipd + imnj + "</String></Pair>");
							}
							else
							{
								nfile.WriteLine("				<Pair key=" + '"' + "prod_pict_path" + '"' + "><String>" + ipd + "download.png</String></Pair>");
							}

							nfile.WriteLine(" 			</Table> \n		</Attributes> \n			<Items>");
							nfile.WriteLine("			<Item class=" + '"' + "type:x-xmb/xmlnpsignup" + '"' + " key=" + '"' + "link" + t + '"' + " attr=" + '"' + "link" + t + '"' + "/>");
							nfile.WriteLine("		</Items> \n		</View> \n\n");



							count++;

							i++;
						}
					}
				}
				nfile.WriteLine("</XMBML>");
				Patch_local_xml();
				Patch_server_setup_xml();
			}


			if (xtype == "usb")
			{
				//toolStripLabel1.Text = "";
				//MessageBox.Show("Saved " + appPath + "bin\\package_link.xml \nSaved " + pkg + "\\Package_List.pkg ", "File Saved", MessageBoxButtons.OK);
			}
			else if (xtype == "hdd")
			{
				if ((pl == 4) || (pl == 6) || (pl == 128))
				{
				if (File.Exists("bin/PS3_GAME/USRDIR/package_link.xml"))
				{
					File.Delete("bin/PS3_GAME/USRDIR/package_link.xml");
				}
				File.Move("bin/package_link.xml", "bin/PS3_GAME/USRDIR/package_link.xml");


					// Use ProcessStartInfo class.
					ProcessStartInfo startInfo = new ProcessStartInfo();
					startInfo.CreateNoWindow = false;//true;
					startInfo.UseShellExecute = false;
					startInfo.WorkingDirectory = appPath;// + "bin/";// +"\\bin";
														 //startInfo.RedirectStandardError = true;
														 // startInfo.RedirectStandardInput = true;
														 //startInfo.RedirectStandardOutput = true;
					startInfo.FileName = "bin/pkg.py";
					startInfo.WindowStyle = ProcessWindowStyle.Hidden;
					startInfo.Arguments = "--contentid UP0100-PKGLINKER_00-PINK1000DEVIL303 bin/PS3_GAME/ bin/UP0100-PKGLINKER_00-PINK1000DEVIL303.pkg";
					// Start the process with the info we specified.
					// Call WaitForExit and then the using-statement will close.
					Process exeProcess = Process.Start(startInfo);
					exeProcess.WaitForExit();


					if (o == 1)
					{



						// Use ProcessStartInfo class.
						ProcessStartInfo startInfo2 = new ProcessStartInfo();
						startInfo2.CreateNoWindow = false;//true;
						startInfo2.UseShellExecute = false;
						startInfo2.WorkingDirectory = appPath + "";// + "\\bin";
																   //startInfo2.RedirectStandardError = true;
						startInfo2.RedirectStandardInput = true;
						startInfo2.RedirectStandardOutput = true;
						startInfo2.FileName = appPath + "bin/ps3xploit_rifgen_edatresign"; //UP0100-PKGLINKER_00-PINK1000DE";L//3.pkg h";
						startInfo2.WindowStyle = ProcessWindowStyle.Hidden;
						startInfo2.Arguments = appPath + "bin/UP0100-PKGLINKER_00-PINK1000DEVIL303.pkg";
						// Start the process with the info we specified.
						// Call WaitForExit and then the using-statement will close.
						Process exeProcess2 = Process.Start(startInfo2);



						while (exeProcess2.HasExited == false)
						{
							// exeProcess2.StandardInput.WriteLine("");
						}
						exeProcess2.WaitForExit();


						//MessageBox.Show("Saved " + pkg + "\\UP0100-PKGLINKER_00-DEVIL303000PINK1.pkg", "File Saved", MessageBoxButtons.OK);
					}

					if (o == 0)
					{
						if (File.Exists("bin/UP0100-PKGLINKER_00-PINK1000DEVIL303.pkg"))
						{
							if (File.Exists(pkg + "/Package_List.pkg"))
							{
								File.Delete(pkg + "/Package_List.pkg");
							}
							File.Move("bin/UP0100-PKGLINKER_00-PINK1000DEVIL303.pkg", pkg + "/Package_List.pkg");
						}
					}

					else if (o == 1)
					{

						if (File.Exists("bin/UP0100-PKGLINKER_00-PINK1000DEVIL303.pkg_signed.pkg"))
						{
							if (File.Exists(pkg + "/bin/UP0100-PKGLINKER_00-PINK1000DEVIL303.pkg"))
							{
								File.Delete(pkg + "/bin/UP0100-PKGLINKER_00-PINK1000DEVIL303.pkg");
							}
							if (File.Exists(pkg + "/Package_List.pkg"))
							{
								File.Delete(pkg + "/Package_List.pkg");
							}
							File.Move("bin/UP0100-PKGLINKER_00-PINK1000DEVIL303.pkg_signed.pkg", pkg + "/Package_List.pkg");
						}
					}
				}


				else
				{
					if (File.Exists("bin/PS3_GAME/USRDIR/package_link.xml"))
                    {
                        File.Delete("bin/PS3_GAME/USRDIR/package_link.xml");
                    }
                    File.Move("bin/package_link.xml", "bin/PS3_GAME/USRDIR/package_link.xml");

					// Use ProcessStartInfo class.
					ProcessStartInfo startInfo = new ProcessStartInfo();
					startInfo.CreateNoWindow = false;//true;
					startInfo.UseShellExecute = false;
					startInfo.WorkingDirectory = appPath + "";
					//startInfo.RedirectStandardError = true;
					// startInfo.RedirectStandardInput = true;
					//startInfo.RedirectStandardOutput = true;
					startInfo.FileName = "bin/CREATE_PKG.bat";
					startInfo.WindowStyle = ProcessWindowStyle.Hidden;
					//startInfo.Arguments = "CREATE_PKG.sh";
					// Start the process with the info we specified.
					// Call WaitForExit and then the using-statement will close.
					Process exeProcess = Process.Start(startInfo);
					exeProcess.WaitForExit();


					if (o == 1)
					{



						// Use ProcessStartInfo class.
						ProcessStartInfo startInfo2 = new ProcessStartInfo();
						startInfo2.CreateNoWindow = true;
						startInfo2.UseShellExecute = false;
						startInfo2.WorkingDirectory = appPath + "";
						//startInfo2.RedirectStandardError = true;
						startInfo2.RedirectStandardInput = true;
						// startInfo2.RedirectStandardOutput = true;
						startInfo2.FileName = "bin/ps3xploit_rifgen_edatresign.exe"; //UP0100-PKGLINKER_00-PINK1000DE";L//3.pkg h";
						startInfo2.WindowStyle = ProcessWindowStyle.Hidden;
						startInfo2.Arguments = "bin/UP0100-PKGLINKER_00-PINK1000DEVIL303.pkg";
						// Start the process with the info we specified.
						// Call WaitForExit and then the using-statement will close.
						Process exeProcess2 = Process.Start(startInfo2);



						while (exeProcess2.HasExited == false)
						{
							exeProcess2.StandardInput.WriteLine("\r\n");
						}
						exeProcess2.WaitForExit();


						//MessageBox.Show("Saved " + pkg + "\\UP0100-PKGLINKER_00-DEVIL303000PINK1.pkg", "File Saved", MessageBoxButtons.OK);
					}
				}

				if (o == 0)
				{
					if (File.Exists("bin\\UP0100-PKGLINKER_00-PINK1000DEVIL303.pkg"))
					{
						if (File.Exists(pkg + "\\Package_List.pkg"))
						{
							File.Delete(pkg + "\\Package_List.pkg");
						}
						File.Move("bin\\UP0100-PKGLINKER_00-PINK1000DEVIL303.pkg", pkg + "\\Package_List.pkg");
					}
				}

				else if (o == 1)
				{

					if (File.Exists("bin\\UP0100-PKGLINKER_00-PINK1000DEVIL303.pkg_signed.pkg"))
					{
						if (File.Exists(pkg + "\\bin\\UP0100-PKGLINKER_00-PINK1000DEVIL303.pkg"))
						{
							File.Delete(pkg + "\\bin\\UP0100-PKGLINKER_00-PINK1000DEVIL303.pkg");
						}
						if (File.Exists(pkg + "\\Package_List.pkg"))
						{
							File.Delete(pkg + "\\Package_List.pkg");
						}
						File.Move("bin\\UP0100-PKGLINKER_00-PINK1000DEVIL303.pkg_signed.pkg", pkg + "\\Package_List.pkg");
					}

				}
				/*
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    this.progress1.Visibility = Visibility.Collapsed;
                }));*/
				//toolStripLabel1.Text = "";
			}
			//set_load(false);
		}

		private void Rename()
		{
			dinfo = new DirectoryInfo(appPath);
			//files = Directory.GetFiles(appPath, "*.pkg");
			Files = dinfo.GetFiles("*.pkg");

			foreach (FileInfo file in Files)
			{
				string temps = file.ToString();
				s = file.ToString();
				s = s.Replace("&", "and");
				s = s.Replace(" ", "_");
				//s = s.Replace("__", "_");
				if (File.Exists(s) && temps != s)
				{
					File.Delete(s);
				}
				File.Move(temps, s);
			}

		}


		#endregion<<files>>

		#region<<image>>

		private void GenerateImage(string bkImage, string cname)
		{
			System.Drawing.Image backImage = (Bitmap)System.Drawing.Image.FromFile(bkImage);
			int targetHeight = 320; //height and width of the finished image
			int targetWidth = 320;

			//be sure to use a pixelformat that supports transparency
			using (var bitmap = new Bitmap(targetWidth, targetHeight,
					System.Drawing.Imaging.PixelFormat.Format32bppArgb))
			{
				using (var canvas = Graphics.FromImage(bitmap))
				{
					//this ensures that the backgroundcolor is transparent
					canvas.Clear(System.Drawing.Color.Transparent);

					//this selects the entire backimage and and paints
					//it on the new image in the same size, so its not distorted.
					int w = (320 - backImage.Width) / 2;
					int h = (320 - backImage.Height) / 2;
					canvas.DrawImage(backImage,
							  new System.Drawing.Rectangle(w, h, backImage.Width, backImage.Height),
							  new System.Drawing.Rectangle(0, 0, backImage.Width, backImage.Height),
							  GraphicsUnit.Pixel);

					//this paints the frontimage with a offset at the given coordinates
					//canvas.DrawImage(frontImage, 5, 25);

					canvas.Save();
					bitmap.Save(cname, ImageFormat.Png);
					backImage.Dispose();

				}
			}
		}

		private void ExtractIcons2()
		{
			appPath = appPath.Replace("pkg_linker_cmd.exe", "");
			pkg = appPath;
			Rename();
			dinfo = new DirectoryInfo(appPath);
			//files = Directory.GetFiles(appPath, "*.pkg");
			Files = dinfo.GetFiles("*.pkg");

			foreach (FileInfo file in Files)
			{


				Read_header(file.Name);
			}
		}


		private void ExtractIcons()
		{
			appPath = appPath.Replace("pkg_linker_cmd.exe", "");
			pkg = appPath;
			Rename();
			dinfo = new DirectoryInfo(appPath);
			//files = Directory.GetFiles(appPath, "*.pkg");
			Files = dinfo.GetFiles("*.pkg");

			foreach (FileInfo file in Files)
			{

				try
				{
					Read_header(file.Name);

					// Start the process with the info we specified.
					// Call WaitForExit and then the using-statement will close.

					string imn = file.FullName;
					imn = imn.Replace(".PKG", ".PNG");
					imn = imn.Replace(".pkg", ".PNG");
					imn = imn.Replace("&", "and");
					string imo = file.Name;
					imo = imo.Replace(".PKG", ".PNG");
					imo = imo.Replace(".pkg", ".PNG");
					imo = imo.Replace("&", "and");


					if (File.Exists(file.Directory + "\\ICON0.PNG"))
					{
						GenerateImage(file.Directory + "\\ICON0.PNG", imn);
						//File.Move(file.Directory + "\\ICON0.PNG", imn);
						File.Delete(file.Directory + "\\ICON0.PNG");
						if (!File.Exists(file.Directory + "\\bin\\icons\\" + imo) && File.Exists(imn))
						{
							File.Move(imn, file.Directory + "\\bin\\icons\\" + imo);
							//File.Delete(file.Directory + "\\bin\\icons\\" + imo);
						}

						//File.Copy(imn, file.Directory + "\\bin\\PS3_GAME\\USRDIR\\icons\\" + imo);

						//File.Move(imn, file.Directory + "\\bin\\icons\\" + imo);



						if (File.Exists(file.Directory + "\\bin\\icons\\" + imo))
						{
							if (File.Exists(file.Directory + "\\bin\\PS3_GAME\\USRDIR\\icons\\" + imo))
							{
								File.Delete(file.Directory + "\\bin\\PS3_GAME\\USRDIR\\icons\\" + imo);
							}
							File.Copy(file.Directory + "\\bin\\icons\\" + imo, file.Directory + "\\bin\\PS3_GAME\\USRDIR\\icons\\" + imo);

						}

						if (File.Exists(imn))
						{
							File.Delete(imn);
						}
					}


				}

				catch
				{
					// Log error.
				}

			}
		}

		public string Read_header(string PKGFileName)
		{

			if (!File.Exists(PKGFileName))
			{
				Console.WriteLine(PKGFileName + " not found");
				return PKGFileName;
			}
			else
			{
				try
				{
					int moltiplicator = 65536;
					byte[] EncryptedData = new byte[AesKey.Length * moltiplicator];
					byte[] DecryptedData = new byte[AesKey.Length * moltiplicator];


					byte[] item_count = new byte[4];
					byte[] EncryptedFileStartOffset = new byte[4];
					byte[] EncryptedFileLenght = new byte[4];
					string outFile = null;
					Stream PKGReadStream = new FileStream(PKGFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
					BinaryReader brPKG = new BinaryReader(PKGReadStream);

					PKGReadStream.Seek(0x00, SeekOrigin.Begin);
					byte[] pkgMagic = brPKG.ReadBytes(4);
					if (pkgMagic[0x00] != 0x7F || pkgMagic[0x01] != 0x50 || pkgMagic[0x02] != 0x4B || pkgMagic[0x03] != 0x47)
					{
						//      txtResults.Text += "ERROR: Selected file isn't a Pkg file.";
						SystemSounds.Beep.Play();
						return string.Empty;
					}

					//Finalized byte
					PKGReadStream.Seek(0x04, SeekOrigin.Begin);
					byte pkgFinalized = brPKG.ReadByte();

					if (pkgFinalized != 0x80)
					{
						PKGReadStream.Seek(0x60, SeekOrigin.Begin);
						fk = brPKG.ReadBytes(0x08);
						PKGReadStream.Seek(0x68, SeekOrigin.Begin);
						ek = brPKG.ReadBytes(8);
						//      MessageBox.Show("Detected Debug PKG. Currently fixing tool. Please be patient");
						//SystemSounds.Beep.Play();
						// SystemSounds.Beep.Play();
					}

					//PKG Type PSP/PS3
					PKGReadStream.Seek(0x07, SeekOrigin.Begin);
					byte pkgType = brPKG.ReadByte();

					switch (pkgType)
					{
						case 0x01:
							//PS3
							AesKey = PS3AesKey;
							break;

						case 0x02:
							//PSP
							AesKey = PSPAesKey;
							break;

						default:
							//     txtResults.Text += "ERROR: Selected pkg isn't Valid.";
							SystemSounds.Beep.Play();
							return string.Empty;
					}

					//0x14 Store the item_count
					PKGReadStream.Seek(0x14, SeekOrigin.Begin);
					item_count = brPKG.ReadBytes((int)item_count.Length);
					Array.Reverse(item_count);
					uiNumberofFiles = BitConverter.ToUInt32(item_count, 0);


					//0x24 Store the start Address of the encrypted file to decrypt
					PKGReadStream.Seek(0x24, SeekOrigin.Begin);
					EncryptedFileStartOffset = brPKG.ReadBytes((int)EncryptedFileStartOffset.Length);
					Array.Reverse(EncryptedFileStartOffset);
					uiEncryptedFileStartOffset = BitConverter.ToUInt32(EncryptedFileStartOffset, 0);

					//0x1C Store the length of the whole pkg file

					//0x2C Store the length of the encrypted file to decrypt
					PKGReadStream.Seek(0x2C, SeekOrigin.Begin);
					EncryptedFileLenght = brPKG.ReadBytes((int)EncryptedFileLenght.Length);
					Array.Reverse(EncryptedFileLenght);
					uint uiEncryptedFileLenght = BitConverter.ToUInt32(EncryptedFileLenght, 0);

					//0x70 Store the PKG file Key.
					PKGReadStream.Seek(0x70, SeekOrigin.Begin);
					PKGFileKey = brPKG.ReadBytes(16);
					byte[] incPKGFileKey = new byte[16];
					Array.Copy(PKGFileKey, incPKGFileKey, PKGFileKey.Length);


					//the "file" key at 0x70 have to be encrypted with a "global AES key" to generate the "xor" key
					//PSP uses CipherMode.ECB, PaddingMode.None that doesn't need IV
					PKGXorKey = AESEngine.Encrypt(PKGFileKey, AesKey, AesKey, CipherMode.ECB, PaddingMode.None);

					uint tempn = uiNumberofFiles * 16;
					// Pieces calculation
					double division = (double)tempn / (double)AesKey.Length;
					UInt64 pieces = (UInt64)Math.Floor(division);
					UInt64 mod = (UInt64)tempn / (UInt64)AesKey.Length;
					if (mod > 0)
						pieces += 1;

					if (File.Exists(PKGFileName + ".Dec"))
					{
						File.Delete(PKGFileName + ".Dec");
					}
					if (pkgFinalized == 0x80)
					{
						// Console.WriteLine("Decrypting");
						//Write File
						FileStream DecryptedFileWriteStream = new FileStream(PKGFileName + ".Dec", FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
						BinaryWriter bwDecryptedFile = new BinaryWriter(DecryptedFileWriteStream);

						//Put the read pointer on the encrypted starting point.
						PKGReadStream.Seek((int)uiEncryptedFileStartOffset, SeekOrigin.Begin);

						// Pieces calculation
						double filedivision = (double)tempn / (double)(AesKey.Length * moltiplicator);
						UInt64 filepieces = (UInt64)Math.Floor(filedivision);
						UInt64 filemod = (UInt64)tempn % (UInt64)(AesKey.Length * moltiplicator);
						if (filemod > 0)
							filepieces += 1;

						//     progressBar.Value = 0;
						//     progressBar.Maximum = (int)filepieces - 1;
						//    progressBar.Step = 1;
						//   Application.DoEvents();

						for (UInt64 i = 0; i < 10; i++)
						{
							//If we have a mod and this is the last piece then...
							if ((filemod > 0) && (i == (filepieces - 1)))
							{
								EncryptedData = new byte[filemod];
								DecryptedData = new byte[filemod];
							}

							//Read 16 bytes of Encrypted data
							EncryptedData = brPKG.ReadBytes(EncryptedData.Length);

							//In order to retrieve a fast AES Encryption we pre-Increment the array
							byte[] PKGFileKeyConsec = new byte[EncryptedData.Length];
							byte[] PKGXorKeyConsec = new byte[EncryptedData.Length];
							//Console.WriteLine("Decrypting");
							for (int pos = 0; pos < EncryptedData.Length; pos += AesKey.Length)
							{

								Array.Copy(incPKGFileKey, 0, PKGFileKeyConsec, pos, PKGFileKey.Length);

								IncrementArray(ref incPKGFileKey, PKGFileKey.Length - 1);
							}
							// Console.WriteLine("Decrypting");
							//the incremented "file" key have to be encrypted with a "global AES key" to generate the "xor" key
							//PSP uses CipherMode.ECB, PaddingMode.None that doesn't need IV
							PKGXorKeyConsec = AESEngine.Encrypt(PKGFileKeyConsec, AesKey, AesKey, CipherMode.ECB, PaddingMode.None);

							//XOR Decrypt and save every 16 bytes of data:
							DecryptedData = XOREngine.XOR(EncryptedData, 0, PKGXorKeyConsec.Length, PKGXorKeyConsec);

							//    progressBar.PerformStep();
							//Application.DoEvents();

							bwDecryptedFile.Write(DecryptedData);

						}
						// Application.DoEvents();

						DecryptedFileWriteStream.Close();
						bwDecryptedFile.Close();


						outFile = ExtractFiles(PKGFileName + ".Dec", PKGFileName);
					}
					else
					{
						if (File.Exists(PKGFileName + ".Dec"))
						{
							File.Delete(PKGFileName + ".Dec");
						}
						Read(PKGFileName);
						//string outFile = null;
						outFile = ExtractDFiles(PKGFileName + ".Dec", PKGFileName);
					}
					PKGReadStream.Close();
					brPKG.Close();
					return outFile;

				}
				catch (Exception ex)
				{
					//   txtResults.Text += "ERROR: An error occured during the decrypting process ";
					//SystemSounds.Beep.Play();
					return string.Empty;
				}
			}
		}

		private byte[] DecryptData(int dataSize, long dataRelativeOffset, long pkgEncryptedFileStartOffset, byte[] AesKey, Stream encrPKGReadStream, BinaryReader brEncrPKG)
		{
			int size = dataSize % 16;
			if (size > 0)
				size = ((dataSize / 16) + 1) * 16;
			else
				size = dataSize;

			byte[] EncryptedData = new byte[size];
			byte[] DecryptedData = new byte[size];
			byte[] PKGFileKeyConsec = new byte[size];
			byte[] PKGXorKeyConsec = new byte[size];
			byte[] incPKGFileKey = new byte[PKGFileKey.Length];
			Array.Copy(PKGFileKey, incPKGFileKey, PKGFileKey.Length);

			encrPKGReadStream.Seek(dataRelativeOffset + pkgEncryptedFileStartOffset, SeekOrigin.Begin);
			EncryptedData = brEncrPKG.ReadBytes(size);

			for (int pos = 0; pos < dataRelativeOffset; pos += 16)
			{
				IncrementArray(ref incPKGFileKey, PKGFileKey.Length - 1);
			}

			for (int pos = 0; pos < size; pos += 16)
			{
				Array.Copy(incPKGFileKey, 0, PKGFileKeyConsec, pos, PKGFileKey.Length);

				IncrementArray(ref incPKGFileKey, PKGFileKey.Length - 1);
			}

			//the incremented "file" key have to be encrypted with a "global AES key" to generate the "xor" key
			//PSP uses CipherMode.ECB, PaddingMode.None that doesn't need IV
			PKGXorKeyConsec = AESEngine.Encrypt(PKGFileKeyConsec, AesKey, AesKey, CipherMode.ECB, PaddingMode.None);

			//XOR Decrypt and save every 16 bytes of data:
			DecryptedData = XOREngine.XOR(EncryptedData, 0, PKGXorKeyConsec.Length, PKGXorKeyConsec);

			return DecryptedData;
		}

		public static void Read(string PKGFileName)
		{
			Stream PKGReadStream = new FileStream(PKGFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			BinaryReader brPKG = new BinaryReader(PKGReadStream);

			PKGReadStream.Seek(0x20, SeekOrigin.Begin);
			byte[] bstart = brPKG.ReadBytes(0x08);
			Array.Reverse(bstart);
			start = BitConverter.ToUInt32(bstart, 0);

			PKGReadStream.Seek(0x28, SeekOrigin.Begin);
			byte[] bsize = brPKG.ReadBytes(0x08);
			Array.Reverse(bsize);
			size = BitConverter.ToUInt32(bsize, 0);

			PKGReadStream.Seek(0x60, SeekOrigin.Begin);
			fk = brPKG.ReadBytes(0x08);
			PKGReadStream.Seek(0x68, SeekOrigin.Begin);
			ek = brPKG.ReadBytes(8);
			PKGReadStream.Seek(start, SeekOrigin.Begin);//(384, SeekOrigin.Begin);
			byte[] pkg = brPKG.ReadBytes((int)size);



			Decrypt(pkg, PKGFileName + ".Dec");

			PKGReadStream.Close();
			brPKG.Close();
		}

		public static void Decrypt(byte[] pkg, string name)
		{
			for (int c = 0; c < key.Length; c++)
			{
				key[c] = 0x00;
			}
			Array.Copy(fk, 0, key, 0, fk.Length);
			Array.Copy(fk, 0, key, 8, fk.Length);
			Array.Copy(ek, 0, key, 16, ek.Length);
			Array.Copy(ek, 0, key, 24, ek.Length);
			Array.Copy(key, 0x38, temp, 0, temp.Length);
			byte[] bfr = new byte[0x1c];


			SHA1 sha1 = new SHA1CryptoServiceProvider();

			uint utemp = BitConverter.ToUInt32(temp, 0) + offset;

			temp = BitConverter.GetBytes(utemp);
			int v = 8 - temp.Length;
			Array.Reverse(temp);
			Array.Copy(temp, 0, key, 56 + v, temp.Length);
			bfr = sha1.ComputeHash(key, 0, 0x40);

			uint i = 0;
			for (i = 0; i < size; i++)
			{
				if (i != 0 && (i % 16) == 0)
				{
					Array.Reverse(temp);
					utemp = BitConverter.ToUInt32(temp, 0) + 1;

					temp = BitConverter.GetBytes(utemp);
					v = 8 - temp.Length;
					Array.Reverse(temp);
					Array.Copy(temp, 0, key, 56 + v, temp.Length);
					bfr = sha1.ComputeHash(key, 0, 0x40);
				}
				pkg[i] ^= bfr[i & 0xf];

			}

			FileStream DecryptedFileWriteStream2 = new FileStream(name, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
			BinaryWriter bwDecryptedFile2 = new BinaryWriter(DecryptedFileWriteStream2);

			bwDecryptedFile2.Write(pkg);
			DecryptedFileWriteStream2.Close();
			bwDecryptedFile2.Close();

		}

		public static void Decrypt2(byte[] pkg, string name, int dataSize, long dataRelativeOffset, long pkgEncryptedFileStartOffset, string PKGFileName)
		{
			for (int c = 0; c < key.Length; c++)
			{
				key[c] = 0x00;
			}
			Array.Copy(fk, 0, key, 0, fk.Length);
			Array.Copy(fk, 0, key, 8, fk.Length);
			Array.Copy(ek, 0, key, 16, ek.Length);
			Array.Copy(ek, 0, key, 24, ek.Length);
			Array.Copy(key, 0x38, temp, 0, temp.Length);
			byte[] bfr = new byte[0x1c];


			SHA1 sha1 = new SHA1CryptoServiceProvider();

			uint utemp = BitConverter.ToUInt32(temp, 0) + offset;

			temp = BitConverter.GetBytes(utemp);
			int v = 8 - temp.Length;
			Array.Reverse(temp);
			Array.Copy(temp, 0, key, 56 + v, temp.Length);
			bfr = sha1.ComputeHash(key, 0, 0x40);

			uint i = 0;
			for (i = 0; i < size; i++)
			{
				if (i != 0 && (i % 16) == 0)
				{
					Array.Reverse(temp);
					utemp = BitConverter.ToUInt32(temp, 0) + 1;

					temp = BitConverter.GetBytes(utemp);
					v = 8 - temp.Length;
					Array.Reverse(temp);
					Array.Copy(temp, 0, key, 56 + v, temp.Length);
					bfr = sha1.ComputeHash(key, 0, 0x40);
				}
				pkg[i] ^= bfr[i & 0xf];

			}

			FileStream DecryptedFileWriteStream2 = new FileStream(name, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
			BinaryWriter bwDecryptedFile2 = new BinaryWriter(DecryptedFileWriteStream2);

			bwDecryptedFile2.Write(pkg);
			DecryptedFileWriteStream2.Close();
			bwDecryptedFile2.Close();

		}


		private byte[] DecryptdData(int dataSize, long dataRelativeOffset, long pkgEncryptedFileStartOffset, string PKGFileName, Stream encrPKGReadStream, BinaryReader brEncrPKG)
		{

			byte[] EncryptedData = new byte[dataSize];
			byte[] DecryptedData = new byte[dataSize];

			//encrPKGReadStream.Seek(dataRelativeOffset + pkgEncryptedFileStartOffset, SeekOrigin.Begin);
			//EncryptedData = brEncrPKG.ReadBytes(size);

			encrPKGReadStream.Seek(0x20, SeekOrigin.Begin);
			byte[] bstart = brEncrPKG.ReadBytes(0x08);
			Array.Reverse(bstart);
			start = BitConverter.ToUInt32(bstart, 0);

			encrPKGReadStream.Seek(0x28, SeekOrigin.Begin);
			byte[] bsize = brEncrPKG.ReadBytes(0x08);
			Array.Reverse(bsize);
			//size = BitConverter.ToUInt32(bsize, 0);

			encrPKGReadStream.Seek(0x60, SeekOrigin.Begin);
			fk = brEncrPKG.ReadBytes(0x08);
			encrPKGReadStream.Seek(0x68, SeekOrigin.Begin);
			ek = brEncrPKG.ReadBytes(8);
			//encrPKGReadStream.Seek(pkgEncryptedFileStartOffset + dataRelativeOffset, SeekOrigin.Begin);//(384, SeekOrigin.Begin);
			//byte[] pkg = brEncrPKG.ReadBytes(size);// + (int)dataRelativeOffset);

			encrPKGReadStream.Seek(dataRelativeOffset + pkgEncryptedFileStartOffset, SeekOrigin.Begin);
			EncryptedData = brEncrPKG.ReadBytes(dataSize);
			byte[] pkg = EncryptedData;



			// decrypt2(pkg, PKGFileName + ".Dec", dataSize, dataRelativeOffset, pkgEncryptedFileStartOffset, PKGFileName);

			// PKGReadStream.Close();
			//brPKG.Close();

			for (int c = 0; c < key.Length; c++)
			{
				key[c] = 0x00;
			}
			Array.Copy(fk, 0, key, 0, fk.Length);
			Array.Copy(fk, 0, key, 8, fk.Length);
			Array.Copy(ek, 0, key, 16, ek.Length);
			Array.Copy(ek, 0, key, 24, ek.Length);
			Array.Copy(key, 0x38, temp, 0, temp.Length);
			byte[] bfr = new byte[0x1c];



			SHA1 sha1 = new SHA1CryptoServiceProvider();

			uint utemp = BitConverter.ToUInt32(temp, 0) + offset;

			temp = BitConverter.GetBytes(utemp);
			int v = 8 - temp.Length;
			Array.Reverse(temp);
			Array.Copy(temp, 0, key, 56 + v, temp.Length);
			bfr = sha1.ComputeHash(key, 0, 0x40);

			uint w = 0;
			for (w = 0; w < (int)dataRelativeOffset; w++)
			{
				if (w != 0 && (w % 16) == 0)
				{
					Array.Reverse(temp);
					utemp = BitConverter.ToUInt32(temp, 0) + 1;

					temp = BitConverter.GetBytes(utemp);
					v = 8 - temp.Length;
					Array.Reverse(temp);
					Array.Copy(temp, 0, key, 56 + v, temp.Length);
					bfr = sha1.ComputeHash(key, 0, 0x40);
				}


			}
			uint i = 0;
			for (i = 0; i < dataSize; i++, w++)
			{
				if (w != 0 && (w % 16) == 0)
				{
					Array.Reverse(temp);
					utemp = BitConverter.ToUInt32(temp, 0) + 1;

					temp = BitConverter.GetBytes(utemp);
					v = 8 - temp.Length;
					Array.Reverse(temp);
					Array.Copy(temp, 0, key, 56 + v, temp.Length);
					bfr = sha1.ComputeHash(key, 0, 0x40);
				}
				pkg[i] ^= bfr[w & 0xf];

			}

			//FileStream DecryptedFileWriteStream2 = new FileStream( filename, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
			//BinaryWriter bwDecryptedFile2 = new BinaryWriter(DecryptedFileWriteStream2);

			// bwDecryptedFile2.Write(pkg);
			// DecryptedFileWriteStream2.Close();
			// bwDecryptedFile2.Close();


			//EncryptedData = pkg;
			return pkg;
		}

		private string ExtractFiles(string decryptedPKGFileName, string encryptedPKGFileName)
		{
			try
			{
				int twentyMb = 1024 * 1024 * 20;
				UInt32 ExtractedFileOffset = 0;
				UInt32 ExtractedFileSize = 0;
				UInt32 OffsetShift = 0;
				long positionIdx = 0;
				string pkgname = null;
				if (encryptedPKGFileName.Contains("\\"))
				{
					string pkgname1 = encryptedPKGFileName.Substring(encryptedPKGFileName.LastIndexOf("\\"));
					string pkgname2 = pkgname1.Replace("\\", "");
					pkgname = pkgname2.Replace(".pkg", "");
				}
				else
				{ pkgname = encryptedPKGFileName.Replace(".pkg", ""); }


				String WorkDir = "temp/" + pkgname;

				// WorkDir = decryptedPKGFileName + ".EXT";
				if (File.Exists("ICON0.PNG"))
				{
					File.Delete("ICON0.PNG");
				}

				Directory.CreateDirectory("temp");
				if (Directory.Exists(WorkDir))
				{
					Directory.Delete(WorkDir, true);
					System.Threading.Thread.Sleep(100);

					Directory.CreateDirectory(WorkDir);
					System.Threading.Thread.Sleep(100);
				}

				byte[] FileTable = new byte[320000];
				byte[] dumpFile;
				byte[] sdkVer = new byte[8];
				byte[] firstFileOffset = new byte[4];
				byte[] firstNameOffset = new byte[4];
				byte[] fileNr = new byte[4];
				byte[] isDir = new byte[4];
				byte[] Offset = new byte[4];
				byte[] Size = new byte[4];
				byte[] NameOffset = new byte[4];
				byte[] NameSize = new byte[4];
				byte[] Name = new byte[32];
				byte[] bootMagic = new byte[8];
				byte contentType = 0;
				byte fileType = 0;
				bool isFile = false;

				Stream decrPKGReadStream = new FileStream(decryptedPKGFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				BinaryReader brDecrPKG = new BinaryReader(decrPKGReadStream);

				Stream encrPKGReadStream = new FileStream(encryptedPKGFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				BinaryReader brEncrPKG = new BinaryReader(encrPKGReadStream);

				//Read the file Table
				decrPKGReadStream.Seek((long)0, SeekOrigin.Begin);
				FileTable = brDecrPKG.ReadBytes(FileTable.Length);

				positionIdx = 0;

				OffsetShift = 0;   //Shift Relative to os.raw

				Array.Copy(FileTable, 0, firstNameOffset, 0, firstNameOffset.Length);
				Array.Reverse(firstNameOffset);
				uint uifirstNameOffset = BitConverter.ToUInt32(firstNameOffset, 0);

				uint uiFileNr = uifirstNameOffset / 32;

				Array.Copy(FileTable, 12, firstFileOffset, 0, firstFileOffset.Length);
				Array.Reverse(firstFileOffset);
				uint uifirstFileOffset = BitConverter.ToUInt32(firstFileOffset, 0);

				//Read the file Table
				decrPKGReadStream.Seek((long)0, SeekOrigin.Begin);
				FileTable = brDecrPKG.ReadBytes((int)uifirstFileOffset);

				//If number of files is negative then something is wrong...
				if ((int)uiFileNr < 0)
				{
					//  txtResults.Text += "ERROR: An error occured during the files extraction process cause of a decryption error";
					//  SystemSounds.Beep.Play();
					return "";
				}



				//Table:
				//0-3         4-7         8-11        12-15       16-19       20-23       24-27       28-31
				//|name loc | |name size| |   NULL  | |file loc | |  NULL   | |file size| |cont type| |   NULL  |

				for (int ii = 0; ii < (int)uiFileNr; ii++)
				{
					Array.Copy(FileTable, positionIdx + 12, Offset, 0, Offset.Length);
					Array.Reverse(Offset);
					ExtractedFileOffset = BitConverter.ToUInt32(Offset, 0) + OffsetShift;

					Array.Copy(FileTable, positionIdx + 20, Size, 0, Size.Length);
					Array.Reverse(Size);
					ExtractedFileSize = BitConverter.ToUInt32(Size, 0);

					Array.Copy(FileTable, positionIdx, NameOffset, 0, NameOffset.Length);
					Array.Reverse(NameOffset);
					uint ExtractedFileNameOffset = BitConverter.ToUInt32(NameOffset, 0);

					Array.Copy(FileTable, positionIdx + 4, NameSize, 0, NameSize.Length);
					Array.Reverse(NameSize);
					uint ExtractedFileNameSize = BitConverter.ToUInt32(NameSize, 0);

					contentType = FileTable[positionIdx + 24];
					fileType = FileTable[positionIdx + 27];

					Name = new byte[ExtractedFileNameSize];
					Array.Copy(FileTable, (int)ExtractedFileNameOffset, Name, 0, ExtractedFileNameSize);
					string ExtractedFileName = ByteArrayToAscii(Name, 0, Name.Length, true);
					// txtResults.Text += "ExtractedFileName = " + ExtractedFileName;
					// txtResults.Text += "ExtractedFileNameOffset = " + ExtractedFileNameOffset;
					//  txtResults.Text += "ExtractedFileNameSize = " + ExtractedFileNameSize;
					string dataString = String.Concat(Name.Select(b => b.ToString("x2")));
					//  txtResults.Text += "name = " + dataString;



					//Write Directory
					if (!Directory.Exists(WorkDir))
					{
						Directory.CreateDirectory(WorkDir);
						System.Threading.Thread.Sleep(100);
					}

					FileStream ExtractedFileWriteStream = null;

					//File / Directory
					if ((fileType == 0x04) && (ExtractedFileSize == 0x00))
						isFile = false;
					else
						isFile = true;

					//contentType == 0x90 = PSP file/dir
					if (contentType == 0x90)
					{
						string FileDir = WorkDir + "\\" + ExtractedFileName;
						FileDir = FileDir.Replace("/", "\\");
						DirectoryInfo FileDirectory = Directory.GetParent(FileDir);

						if (!Directory.Exists(FileDirectory.ToString()))
						{
							Directory.CreateDirectory(FileDirectory.ToString());
						}
						ExtractedFileWriteStream = new FileStream(FileDir, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
					}
					else
					{
						//contentType == (0x80 || 0x00) = PS3 file/dir
						//fileType == 0x01 = NPDRM File
						//fileType == 0x03 = Raw File
						//fileType == 0x04 = Directory

						//Decrypt PS3 Filename
						byte[] DecryptedData = DecryptData((int)ExtractedFileNameSize, (long)ExtractedFileNameOffset, (long)uiEncryptedFileStartOffset, PS3AesKey, encrPKGReadStream, brEncrPKG);
						Array.Copy(DecryptedData, 0, Name, 0, ExtractedFileNameSize);
						ExtractedFileName = ByteArrayToAscii(Name, 0, Name.Length, true);

						StreamReader reader = null;
						// string dataString = String.Concat(FileTable.Select(b => b.ToString("x2")));
						//  sw3.AutoFlush = true;
						//  Console.SetOut(sw3);
						//  Console.WriteLine(ExtractedFileName);
						//   Console.ReadKey();
						//  reader = new StreamReader(ExtractedFileName);                       
						if (!isFile)
						{
							//Directory
							try
							{
								//  if (!Directory.Exists(ExtractedFileName))
								//   Directory.CreateDirectory(WorkDir + "\\" + ExtractedFileName);
							}
							catch (Exception ex)
							{
								//This should not happen xD
								ExtractedFileName = ii.ToString() + ".raw";
								// if (!Directory.Exists(ExtractedFileName))
								//  Directory.CreateDirectory(WorkDir + "\\" + ExtractedFileName);
							}
						}
						else if (ExtractedFileName == "ICON0.PNG")
						{
							//File
							try
							{
								ExtractedFileWriteStream = new FileStream(ExtractedFileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
							}
							catch (Exception ex)
							{
								//This should not happen xD
								ExtractedFileName = ii.ToString() + ".raw";
								// ExtractedFileWriteStream = new FileStream(WorkDir + "\\" + ExtractedFileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
							}
						}
					}

					if (contentType == 0x90 && isFile && ExtractedFileName == "ICON0.PNG")
					{
						//Read/Write File
						BinaryWriter ExtractedFile = new BinaryWriter(ExtractedFileWriteStream);
						decrPKGReadStream.Seek((long)ExtractedFileOffset, SeekOrigin.Begin);

						// Pieces calculation
						double division = (double)ExtractedFileSize / (double)twentyMb;
						UInt64 pieces = (UInt64)Math.Floor(division);
						UInt64 mod = (UInt64)ExtractedFileSize % (UInt64)twentyMb;
						if (mod > 0)
							pieces += 1;

						dumpFile = new byte[twentyMb];
						for (UInt64 i = 0; i < pieces; i++)
						{
							//If we have a mod and this is the last piece then...
							if ((mod > 0) && (i == (pieces - 1)))
								dumpFile = new byte[mod];

							//Fill buffer
							brDecrPKG.Read(dumpFile, 0, dumpFile.Length);

							ExtractedFile.Write(dumpFile);

							//   Application.DoEvents();
						}

						ExtractedFileWriteStream.Close();
						ExtractedFile.Close();
					}


					if (contentType != 0x90 && isFile && ExtractedFileName == "ICON0.PNG")
					//  if (contentType != 0x90 && isFile && Name == Name2)
					{
						//Read/Write File
						BinaryWriter ExtractedFile = new BinaryWriter(ExtractedFileWriteStream);
						decrPKGReadStream.Seek((long)ExtractedFileOffset, SeekOrigin.Begin);

						// Pieces calculation
						double division = (double)ExtractedFileSize / (double)twentyMb;
						UInt64 pieces = (UInt64)Math.Floor(division);
						UInt64 mod = (UInt64)ExtractedFileSize % (UInt64)twentyMb;
						if (mod > 0)
							pieces += 1;

						dumpFile = new byte[twentyMb];
						long elapsed = 0;
						for (UInt64 i = 0; i < pieces; i++)
						{
							//If we have a mod and this is the last piece then...
							if ((mod > 0) && (i == (pieces - 1)))
								dumpFile = new byte[mod];

							//Fill buffer
							byte[] DecryptedData = DecryptData(dumpFile.Length, (long)ExtractedFileOffset + elapsed, (long)uiEncryptedFileStartOffset, PS3AesKey, encrPKGReadStream, brEncrPKG);
							elapsed = +dumpFile.Length;

							//To avoid decryption pad we use dumpFile.Length that's the actual decrypted file size!
							ExtractedFile.Write(DecryptedData, 0, dumpFile.Length);

							//  Application.DoEvents();
						}

						ExtractedFileWriteStream.Close();
						ExtractedFile.Close();
					}

					positionIdx = positionIdx + 32;

					// progressBar.PerformStep();
					// Application.DoEvents();
				}

				//     Application.DoEvents();

				//Close File
				encrPKGReadStream.Close();
				brEncrPKG.Close();

				decrPKGReadStream.Close();
				brDecrPKG.Close();

				//Delete decrypted file
				if (File.Exists(decryptedPKGFileName))
				{
					File.Delete(decryptedPKGFileName);
				}

				//  txtResults.Text += "SUCCESS: Pkg extracted and decrypted successfully.";
				// SystemSounds.Beep.Play();

				//  fileDirBrowser.PathFileDir = "Open Encrypted PKG file here first...";

				// progressBar.Value = 0;
				//Console.WriteLine("Creating EDAT");
				string outFile = null;
				//C00EDAT instance = new C00EDAT();
				//outFile = instance.makeedat(WorkDir + "/PARAM.SFO", outFile);
				//Console.WriteLine(outFile);
				if (Directory.Exists("temp"))
				{
					Directory.Delete("temp", true);
					System.Threading.Thread.Sleep(100);
				}
				return outFile;
			}

			catch (Exception ex)
			{
				// txtResults.Text += "ERROR: An error occured during the files extraction process ";
				SystemSounds.Beep.Play();
				return "";
			}
		}

		private string ExtractDFiles(string decryptedPKGFileName, string encryptedPKGFileName)
		{

			Stream decrPKGReadStream = new FileStream(decryptedPKGFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			BinaryReader brDecrPKG = new BinaryReader(decrPKGReadStream);

			Stream encrPKGReadStream = new FileStream(encryptedPKGFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			BinaryReader brEncrPKG = new BinaryReader(encrPKGReadStream);

			try
			{
				int dfl = 0;
				int twentyMb = 1024 * 1024 * 20;
				UInt32 ExtractedFileOffset = 0;
				UInt32 ExtractedFileSize = 0;
				UInt32 OffsetShift = 0;
				long positionIdx = 0;
				string pkgname = null;
				if (encryptedPKGFileName.Contains("\\"))
				{
					string pkgname1 = encryptedPKGFileName.Substring(encryptedPKGFileName.LastIndexOf("\\"));
					string pkgname2 = pkgname1.Replace("\\", "");
					pkgname = pkgname2.Replace(".pkg", "");
				}
				else
				{ pkgname = encryptedPKGFileName.Replace(".pkg", ""); }


				// WorkDir = decryptedPKGFileName + ".EXT";
				if (File.Exists("ICON0.PNG"))
				{
					File.Delete("ICON0.PNG");
				}


				byte[] FileTable = new byte[320000];
				byte[] dumpFile;
				byte[] sdkVer = new byte[8];
				byte[] firstFileOffset = new byte[4];
				byte[] firstNameOffset = new byte[4];
				byte[] fileNr = new byte[4];
				byte[] isDir = new byte[4];
				byte[] Offset = new byte[4];
				byte[] Size = new byte[4];
				byte[] NameOffset = new byte[4];
				byte[] NameSize = new byte[4];
				byte[] Name = new byte[32];
				byte[] bootMagic = new byte[8];
				byte contentType = 0;
				byte fileType = 0;
				bool isFile = false;

				//Read the file Table
				decrPKGReadStream.Seek((long)0, SeekOrigin.Begin);
				FileTable = brDecrPKG.ReadBytes(FileTable.Length);

				positionIdx = 0;

				OffsetShift = 0;   //Shift Relative to os.raw

				Array.Copy(FileTable, 0, firstNameOffset, 0, firstNameOffset.Length);
				//Array.Reverse(firstNameOffset);
				uint uifirstNameOffset = BitConverter.ToUInt32(firstNameOffset, 0);

				uint uiFileNr = uifirstNameOffset / 32;

				Array.Copy(FileTable, 12, firstFileOffset, 0, firstFileOffset.Length);
				Array.Reverse(firstFileOffset);
				uint uifirstFileOffset = BitConverter.ToUInt32(firstFileOffset, 0);

				//Read the file Table
				decrPKGReadStream.Seek((long)0, SeekOrigin.Begin);
				FileTable = brDecrPKG.ReadBytes((int)uifirstFileOffset);

				//If number of files is negative then something is wrong...
				if ((int)uiFileNr < 0)
				{
					//  txtResults.Text += "ERROR: An error occured during the files extraction process cause of a decryption error";
					//  SystemSounds.Beep.Play();
					return "";
				}



				//Table:
				//0-3         4-7         8-11        12-15       16-19       20-23       24-27       28-31
				//|name loc | |name size| |   NULL  | |file loc | |  NULL   | |file size| |cont type| |   NULL  |

				for (int ii = 0; ii < (int)uiFileNr; ii++)
				{
					Array.Copy(FileTable, positionIdx + 12, Offset, 0, Offset.Length);
					Array.Reverse(Offset);
					ExtractedFileOffset = BitConverter.ToUInt32(Offset, 0) + OffsetShift;

					Array.Copy(FileTable, positionIdx + 20, Size, 0, Size.Length);
					Array.Reverse(Size);
					ExtractedFileSize = BitConverter.ToUInt32(Size, 0);

					Array.Copy(FileTable, positionIdx, NameOffset, 0, NameOffset.Length);
					Array.Reverse(NameOffset);
					uint ExtractedFileNameOffset = BitConverter.ToUInt32(NameOffset, 0);

					Array.Copy(FileTable, positionIdx + 4, NameSize, 0, NameSize.Length);
					Array.Reverse(NameSize);
					uint ExtractedFileNameSize = BitConverter.ToUInt32(NameSize, 0);

					contentType = FileTable[positionIdx + 24];
					fileType = FileTable[positionIdx + 27];

					Name = new byte[ExtractedFileNameSize];
					Array.Copy(FileTable, (int)ExtractedFileNameOffset, Name, 0, ExtractedFileNameSize);
					string ExtractedFileName = ByteArrayToAscii(Name, 0, Name.Length, true);
					// txtResults.Text += "ExtractedFileName = " + ExtractedFileName;
					// txtResults.Text += "ExtractedFileNameOffset = " + ExtractedFileNameOffset;
					//  txtResults.Text += "ExtractedFileNameSize = " + ExtractedFileNameSize;
					string dataString = String.Concat(Name.Select(b => b.ToString("x2")));
					//  txtResults.Text += "name = " + dataString;





					FileStream ExtractedFileWriteStream = null;

					//File / Directory
					if ((fileType == 0x04) && (ExtractedFileSize == 0x00))
						isFile = false;
					else
						isFile = true;

					//contentType == 0x90 = PSP file/dir
					if (contentType == 0x90)
					{
						string FileDir = ExtractedFileName;
						FileDir = FileDir.Replace("/", "\\");
						DirectoryInfo FileDirectory = Directory.GetParent(FileDir);

						if (!Directory.Exists(FileDirectory.ToString()))
						{
							Directory.CreateDirectory(FileDirectory.ToString());
						}
						ExtractedFileWriteStream = new FileStream(FileDir, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
					}
					else
					{
						//contentType == (0x80 || 0x00) = PS3 file/dir
						//fileType == 0x01 = NPDRM File
						//fileType == 0x03 = Raw File
						//fileType == 0x04 = Directory

						//Decrypt PS3 Filename
						byte[] DecryptedData = DecryptdData((int)ExtractedFileNameSize, (long)ExtractedFileNameOffset, (long)uiEncryptedFileStartOffset, encryptedPKGFileName, encrPKGReadStream, brEncrPKG);
						Array.Copy(DecryptedData, 0, Name, 0, ExtractedFileNameSize);
						ExtractedFileName = ByteArrayToAscii(Name, 0, Name.Length, true);

						StreamReader reader = null;
						// string dataString = String.Concat(FileTable.Select(b => b.ToString("x2")));
						//  sw3.AutoFlush = true;
						//  Console.SetOut(sw3);
						//  Console.WriteLine(ExtractedFileName);
						//   Console.ReadKey();
						//  reader = new StreamReader(ExtractedFileName);                       
						if (!isFile)
						{
							//Directory
							try
							{
								//  if (!Directory.Exists(ExtractedFileName))
								//   Directory.CreateDirectory(WorkDir + "\\" + ExtractedFileName);
							}
							catch (Exception ex)
							{
								//This should not happen xD
								ExtractedFileName = ii.ToString() + ".raw";
								// if (!Directory.Exists(ExtractedFileName))
								//  Directory.CreateDirectory(WorkDir + "\\" + ExtractedFileName);
							}
						}
						else if (ExtractedFileName == "ICON0.PNG")
						{
							//File
							try
							{
								ExtractedFileWriteStream = new FileStream(ExtractedFileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
							}
							catch (Exception ex)
							{
								//This should not happen xD
								ExtractedFileName = ii.ToString() + ".raw";
								// ExtractedFileWriteStream = new FileStream(WorkDir + "\\" + ExtractedFileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
							}
						}
					}

					if (contentType == 0x90 && isFile && ExtractedFileName == "ICON0.PNG")
					{
						//Read/Write File
						BinaryWriter ExtractedFile = new BinaryWriter(ExtractedFileWriteStream);
						decrPKGReadStream.Seek((long)ExtractedFileOffset, SeekOrigin.Begin);

						// Pieces calculation
						double division = (double)ExtractedFileSize / (double)twentyMb;
						UInt64 pieces = (UInt64)Math.Floor(division);
						UInt64 mod = (UInt64)ExtractedFileSize % (UInt64)twentyMb;
						if (mod > 0)
							pieces += 1;

						dumpFile = new byte[twentyMb];
						for (UInt64 i = 0; i < pieces; i++)
						{
							//If we have a mod and this is the last piece then...
							if ((mod > 0) && (i == (pieces - 1)))
								dumpFile = new byte[mod];

							//Fill buffer
							brDecrPKG.Read(dumpFile, 0, dumpFile.Length);

							ExtractedFile.Write(dumpFile);

							//   Application.DoEvents();
						}

						ExtractedFileWriteStream.Close();
						ExtractedFile.Close();
					}


					if (contentType != 0x90 && isFile && ExtractedFileName == "ICON0.PNG")
					//  if (contentType != 0x90 && isFile && Name == Name2)
					{
						//Read/Write File
						BinaryWriter ExtractedFile = new BinaryWriter(ExtractedFileWriteStream);
						decrPKGReadStream.Seek((long)ExtractedFileOffset, SeekOrigin.Begin);

						// Pieces calculation
						double division = (double)ExtractedFileSize / (double)twentyMb;
						UInt64 pieces = (UInt64)Math.Floor(division);
						UInt64 mod = (UInt64)ExtractedFileSize % (UInt64)twentyMb;
						if (mod > 0)
							pieces += 1;

						dumpFile = new byte[twentyMb];
						long elapsed = 0;
						for (UInt64 i = 0; i < pieces; i++)
						{
							//If we have a mod and this is the last piece then...
							if ((mod > 0) && (i == (pieces - 1)))
								dumpFile = new byte[mod];

							//Fill buffer
							byte[] DecryptedData = DecryptdData(dumpFile.Length, (long)ExtractedFileOffset + elapsed, (long)uiEncryptedFileStartOffset, encryptedPKGFileName, encrPKGReadStream, brEncrPKG);
							elapsed = +dumpFile.Length;

							//To avoid decryption pad we use dumpFile.Length that's the actual decrypted file size!
							ExtractedFile.Write(DecryptedData, 0, dumpFile.Length);

							//  Application.DoEvents();
						}

						ExtractedFileWriteStream.Close();
						ExtractedFile.Close();
					}

					positionIdx = positionIdx + 32;

					// progressBar.PerformStep();
					// Application.DoEvents();
				}

				//     Application.DoEvents();

				//Close File
				encrPKGReadStream.Close();
				brEncrPKG.Close();

				decrPKGReadStream.Close();
				brDecrPKG.Close();

				//Delete decrypted file
				if (File.Exists(decryptedPKGFileName))
				{
					File.Delete(decryptedPKGFileName);
				}

				//  txtResults.Text += "SUCCESS: Pkg extracted and decrypted successfully.";
				// SystemSounds.Beep.Play();

				//  fileDirBrowser.PathFileDir = "Open Encrypted PKG file here first...";

				// progressBar.Value = 0;
				//Console.WriteLine("Creating EDAT");
				string outFile = null;
				//C00EDAT instance = new C00EDAT();
				//outFile = instance.makeedat(WorkDir + "/PARAM.SFO", outFile);
				//Console.WriteLine(outFile);
				if (Directory.Exists("temp"))
				{
					Directory.Delete("temp", true);
					System.Threading.Thread.Sleep(100);
				}
				return outFile;
			}

			catch (Exception ex)
			{
				// txtResults.Text += "ERROR: An error occured during the files extraction process ";
				//SystemSounds.Beep.Play();
				encrPKGReadStream.Close();
				brEncrPKG.Close();

				decrPKGReadStream.Close();
				brDecrPKG.Close();
				return "";
			}

		}


		private Boolean IncrementArray(ref byte[] sourceArray, int position)
		{
			if (sourceArray[position] == 0xFF)
			{
				if (position != 0)
				{
					if (IncrementArray(ref sourceArray, position - 1))
					{
						sourceArray[position] = 0x00;
						return true;
					}
					else return false; //Maximum reached yet
				}
				else return false; //Maximum reached yet
			}
			else
			{
				sourceArray[position] += 0x01;
				return true;
			}
		}




		public static string HexStringToAscii(string HexString, bool cleanEndOfString)
		{
			try
			{
				string StrValue = "";
				// While there's still something to convert in the hex string
				while (HexString.Length > 0)
				{
					// Use ToChar() to convert each ASCII value (two hex digits) to the actual character
					StrValue += System.Convert.ToChar(System.Convert.ToUInt32(HexString.Substring(0, 2), 16)).ToString();

					// Remove from the hex object the converted value
					HexString = HexString.Substring(2, HexString.Length - 2);
				}
				// Clean String
				if (cleanEndOfString)
					StrValue = StrValue.Replace("\0", "");

				return StrValue;
			}
			catch (Exception ex)
			{
				return null;
			}
		}

		public static string ByteArrayToAscii(byte[] ByteArray, int startPos, int length, bool cleanEndOfString)
		{
			byte[] byteArrayPhrase = new byte[length];
			Array.Copy(ByteArray, startPos, byteArrayPhrase, 0, byteArrayPhrase.Length);
			string hexPhrase = ByteArrayToHexString(byteArrayPhrase);
			return HexStringToAscii(hexPhrase, true);
		}

		public static string ByteArrayToHexString(byte[] ByteArray)
		{
			string HexString = "";
			for (int i = 0; i < ByteArray.Length; ++i)
				HexString += ByteArray[i].ToString("X2"); // +" ";
			return HexString;
		}

		public static byte[] getByteArray(String hexString)
		{
			return decodeHex(hexString.ToCharArray());
		}

		public static byte[] decodeHex(char[] data)
		{
			int len = data.Length;
			if ((len & 0x01) != 0)
			{
				throw new Exception("Odd number of characters.");
			}

			byte[] o = new byte[len >> 1];

			// two characters form the hex value.
			for (int i = 0, j = 0; j < len; i++)
			{
				int f = toDigit(data[j], j) << 4;
				j++;
				f = f | toDigit(data[j], j);
				j++;
				o[i] = (byte)(f & 0xFF);
			}

			return o;
		}

		static int GetIntegerValue(char c, int radix)
		{
			int val = -1;
			if (char.IsDigit(c))
				val = (int)(c - '0');
			else if (char.IsLower(c))
				val = (int)(c - 'a') + 10;
			else if (char.IsUpper(c))
				val = (int)(c - 'A') + 10;
			if (val >= radix)
				val = -1;
			return val;
		}

		protected static int toDigit(char ch, int index)
		{
			int digit = GetIntegerValue(ch, 16);
			if (digit == -1)
			{
				throw new Exception("Illegal hexadecimal character " + ch + " at index " + index);
			}
			return digit;
		}
		#endregion<<image>>
	}


	public class AESEngine
	{
		// Encrypt a byte array into a byte array using a key and an IV 
		// ECB does not need an IV.
		public static byte[] Encrypt(byte[] clearData, byte[] Key, byte[] IV, CipherMode cipherMode, PaddingMode paddingMode)
		{
			// Create a MemoryStream to accept the encrypted bytes 
			MemoryStream ms = new MemoryStream();

			// Create a symmetric algorithm. 
			// We are going to use Rijndael because it is strong and
			// available on all platforms. 
			// You can use other algorithms, to do so substitute the
			// next line with something like 
			//      TripleDES alg = TripleDES.Create(); 

			Rijndael alg = Rijndael.Create();

			// Now set the key and the IV. 
			// We need the IV (Initialization Vector) because
			// the algorithm is operating in its default 
			// mode called CBC (Cipher Block Chaining).
			// The IV is XORed with the first block (8 byte) 
			// of the data before it is encrypted, and then each
			// encrypted block is XORed with the 
			// following block of plaintext.
			// This is done to make encryption more secure. 

			alg.Mode = cipherMode;
			alg.Padding = paddingMode;
			alg.Key = Key;
			alg.IV = IV;

			// Create a CryptoStream through which we are going to be
			// pumping our data. 
			// CryptoStreamMode.Write means that we are going to be
			// writing data to the stream and the output will be written
			// in the MemoryStream we have provided. 

			CryptoStream cs = new CryptoStream(ms,
			   alg.CreateEncryptor(), CryptoStreamMode.Write);

			// Write the data and make it do the encryption 
			cs.Write(clearData, 0, clearData.Length);

			// Close the crypto stream (or do FlushFinalBlock). 
			// This will tell it that we have done our encryption and
			// there is no more data coming in, 
			// and it is now a good time to apply the padding and
			// finalize the encryption process. 

			cs.Close();

			// Now get the encrypted data from the MemoryStream.
			// Some people make a mistake of using GetBuffer() here,
			// which is not the right way. 

			byte[] encryptedData = ms.ToArray();

			return encryptedData;
		}

		// Encrypt a string into a string using a password
		public static string Encrypt(string clearText, string Password, CipherMode cipherMode, PaddingMode paddingMode)
		{
			// First we need to turn the input string into a byte array. 
			byte[] clearBytes =
			  System.Text.Encoding.Unicode.GetBytes(clearText);

			// Then, we need to turn the password into Key and IV 
			// We are using salt to make it harder to guess our key
			// using a dictionary attack - 
			// trying to guess a password by enumerating all possible words. 

			PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
				new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d,
				0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});

			// Now get the key/IV and do the encryption using the
			// function that accepts byte arrays. 
			// Using PasswordDeriveBytes object we are first getting
			// 32 bytes for the Key 
			// (the default Rijndael key length is 256bit = 32bytes)
			// and then 16 bytes for the IV. 
			// IV should always be the block size, which is by default
			// 16 bytes (128 bit) for Rijndael. 
			// If you are using DES/TripleDES/RC2 the block size is
			// 8 bytes and so should be the IV size. 
			// You can also read KeySize/BlockSize properties off
			// the algorithm to find out the sizes. 

			byte[] encryptedData = Encrypt(clearBytes,
					 pdb.GetBytes(32), pdb.GetBytes(16), cipherMode, paddingMode);

			// Now we need to turn the resulting byte array into a string. 
			// A common mistake would be to use an Encoding class for that.
			//It does not work because not all byte values can be
			// represented by characters. 
			// We are going to be using Base64 encoding that is designed
			//exactly for what we are trying to do. 

			return Convert.ToBase64String(encryptedData);

		}

		// Encrypt bytes into bytes using a password 
		public static byte[] Encrypt(byte[] clearData, string Password, CipherMode cipherMode, PaddingMode paddingMode)
		{
			// We need to turn the password into Key and IV. 
			// We are using salt to make it harder to guess our key
			// using a dictionary attack - 
			// trying to guess a password by enumerating all possible words. 

			PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
				new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d,
				0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});

			// Now get the key/IV and do the encryption using the function
			// that accepts byte arrays. 
			// Using PasswordDeriveBytes object we are first getting
			// 32 bytes for the Key 
			// (the default Rijndael key length is 256bit = 32bytes)
			// and then 16 bytes for the IV. 
			// IV should always be the block size, which is by default
			// 16 bytes (128 bit) for Rijndael. 
			// If you are using DES/TripleDES/RC2 the block size is 8
			// bytes and so should be the IV size. 
			// You can also read KeySize/BlockSize properties off the
			// algorithm to find out the sizes. 

			return Encrypt(clearData, pdb.GetBytes(32), pdb.GetBytes(16), cipherMode, paddingMode);
		}

		// Encrypt a file into another file using a password 

		public static void Encrypt(string fileIn, string fileOut, string Password, CipherMode cipherMode, PaddingMode paddingMode)
		{
			// First we are going to open the file streams 
			FileStream fsIn = new FileStream(fileIn, FileMode.Open, FileAccess.Read);
			FileStream fsOut = new FileStream(fileOut, FileMode.OpenOrCreate, FileAccess.Write);

			// Then we are going to derive a Key and an IV from the
			// Password and create an algorithm 

			PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
				new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d,
				0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});

			Rijndael alg = Rijndael.Create();

			alg.Mode = cipherMode;
			alg.Padding = paddingMode;
			alg.Key = pdb.GetBytes(32);
			alg.IV = pdb.GetBytes(16);

			// Now create a crypto stream through which we are going
			// to be pumping data. 
			// Our fileOut is going to be receiving the encrypted bytes. 

			CryptoStream cs = new CryptoStream(fsOut, alg.CreateEncryptor(), CryptoStreamMode.Write);

			// Now will will initialize a buffer and will be processing
			// the input file in chunks. 
			// This is done to avoid reading the whole file (which can
			// be huge) into memory. 

			int bufferLen = 4096;
			byte[] buffer = new byte[bufferLen];
			int bytesRead;

			do
			{
				// read a chunk of data from the input file 
				bytesRead = fsIn.Read(buffer, 0, bufferLen);

				// encrypt it 
				cs.Write(buffer, 0, bytesRead);
			} while (bytesRead != 0);

			// close everything 
			// this will also close the unrelying fsOut stream
			cs.Close();
			fsIn.Close();
		}

		// Decrypt a byte array into a byte array using a key and an IV 
		public static byte[] Decrypt(byte[] cipherData, byte[] Key, byte[] IV, CipherMode cipherMode, PaddingMode paddingMode)
		{
			// Create a MemoryStream that is going to accept the
			// decrypted bytes 
			MemoryStream ms = new MemoryStream();

			// Create a symmetric algorithm. 
			// We are going to use Rijndael because it is strong and
			// available on all platforms. 
			// You can use other algorithms, to do so substitute the next
			// line with something like 
			//     TripleDES alg = TripleDES.Create(); 

			Rijndael alg = Rijndael.Create();

			// Now set the key and the IV. 
			// We need the IV (Initialization Vector) because the algorithm
			// is operating in its default 
			// mode called CBC (Cipher Block Chaining). The IV is XORed with
			// the first block (8 byte) 
			// of the data after it is decrypted, and then each decrypted
			// block is XORed with the previous 
			// cipher block. This is done to make encryption more secure. 
			// There is also a mode called ECB which does not need an IV,
			// but it is much less secure. 

			alg.Mode = cipherMode;
			alg.Padding = paddingMode;
			alg.Key = Key;
			alg.IV = IV;

			// Create a CryptoStream through which we are going to be
			// pumping our data. 
			// CryptoStreamMode.Write means that we are going to be
			// writing data to the stream 
			// and the output will be written in the MemoryStream
			// we have provided. 

			CryptoStream cs = new CryptoStream(ms,
				alg.CreateDecryptor(), CryptoStreamMode.Write);

			// Write the data and make it do the decryption 
			cs.Write(cipherData, 0, cipherData.Length);

			// Close the crypto stream (or do FlushFinalBlock). 
			// This will tell it that we have done our decryption
			// and there is no more data coming in, 
			// and it is now a good time to remove the padding
			// and finalize the decryption process. 

			cs.Close();

			// Now get the decrypted data from the MemoryStream. 
			// Some people make a mistake of using GetBuffer() here,
			// which is not the right way. 

			byte[] decryptedData = ms.ToArray();

			return decryptedData;
		}

		// Decrypt a string into a string using a password 
		public static string Decrypt(string cipherText, string Password, CipherMode cipherMode, PaddingMode paddingMode)
		{
			// First we need to turn the input string into a byte array. 
			// We presume that Base64 encoding was used 

			byte[] cipherBytes = Convert.FromBase64String(cipherText);

			// Then, we need to turn the password into Key and IV 
			// We are using salt to make it harder to guess our key
			// using a dictionary attack - 
			// trying to guess a password by enumerating all possible words. 

			PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
				new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65,
			0x64, 0x76, 0x65, 0x64, 0x65, 0x76});

			// Now get the key/IV and do the decryption using
			// the function that accepts byte arrays. 
			// Using PasswordDeriveBytes object we are first
			// getting 32 bytes for the Key 
			// (the default Rijndael key length is 256bit = 32bytes)
			// and then 16 bytes for the IV. 
			// IV should always be the block size, which is by
			// default 16 bytes (128 bit) for Rijndael. 
			// If you are using DES/TripleDES/RC2 the block size is
			// 8 bytes and so should be the IV size. 
			// You can also read KeySize/BlockSize properties off
			// the algorithm to find out the sizes. 

			byte[] decryptedData = Decrypt(cipherBytes,
				pdb.GetBytes(32), pdb.GetBytes(16), cipherMode, paddingMode);

			// Now we need to turn the resulting byte array into a string. 
			// A common mistake would be to use an Encoding class for that.
			// It does not work 
			// because not all byte values can be represented by characters. 
			// We are going to be using Base64 encoding that is 
			// designed exactly for what we are trying to do. 

			return System.Text.Encoding.Unicode.GetString(decryptedData);
		}

		// Decrypt bytes into bytes using a password 
		public static byte[] Decrypt(byte[] cipherData, string Password, CipherMode cipherMode, PaddingMode paddingMode)
		{
			// We need to turn the password into Key and IV. 
			// We are using salt to make it harder to guess our key
			// using a dictionary attack - 
			// trying to guess a password by enumerating all possible words. 

			PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
				new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d,
			0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});

			// Now get the key/IV and do the Decryption using the 
			//function that accepts byte arrays. 
			// Using PasswordDeriveBytes object we are first getting
			// 32 bytes for the Key 
			// (the default Rijndael key length is 256bit = 32bytes)
			// and then 16 bytes for the IV. 
			// IV should always be the block size, which is by default
			// 16 bytes (128 bit) for Rijndael. 
			// If you are using DES/TripleDES/RC2 the block size is
			// 8 bytes and so should be the IV size. 

			// You can also read KeySize/BlockSize properties off the
			// algorithm to find out the sizes. 
			return Decrypt(cipherData, pdb.GetBytes(32), pdb.GetBytes(16), cipherMode, paddingMode);
		}

		// Decrypt a file into another file using a password 
		public static void Decrypt(string fileIn, string fileOut, string Password, CipherMode cipherMode, PaddingMode paddingMode)
		{

			// First we are going to open the file streams 

			FileStream fsIn = new FileStream(fileIn,
						FileMode.Open, FileAccess.Read);
			FileStream fsOut = new FileStream(fileOut,
						FileMode.OpenOrCreate, FileAccess.Write);

			// Then we are going to derive a Key and an IV from
			// the Password and create an algorithm 

			PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
				new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d,
			0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});
			Rijndael alg = Rijndael.Create();

			alg.Mode = cipherMode;
			alg.Padding = paddingMode;
			alg.Key = pdb.GetBytes(32);
			alg.IV = pdb.GetBytes(16);

			// Now create a crypto stream through which we are going
			// to be pumping data. 
			// Our fileOut is going to be receiving the Decrypted bytes. 

			CryptoStream cs = new CryptoStream(fsOut, alg.CreateDecryptor(), CryptoStreamMode.Write);

			// Now will will initialize a buffer and will be 
			// processing the input file in chunks. 
			// This is done to avoid reading the whole file (which can be
			// huge) into memory. 

			int bufferLen = 4096;
			byte[] buffer = new byte[bufferLen];
			int bytesRead;

			do
			{
				// read a chunk of data from the input file 
				bytesRead = fsIn.Read(buffer, 0, bufferLen);

				// Decrypt it 
				cs.Write(buffer, 0, bytesRead);

			} while (bytesRead != 0);

			// close everything 
			cs.Close(); // this will also close the unrelying fsOut stream 
			fsIn.Close();
		}
	}


	public class XOREngine
	{
		public static byte[] XOR(byte[] inByteArray, int offsetPos, int length, byte[] XORKey)
		{
			if (inByteArray.Length < offsetPos + length)
				throw new Exception("Combination of chosen offset pos. & Length goes outside of the array to be xored.");

			if ((length % XORKey.Length) != 0)
				throw new Exception("Nr bytes to be xored isn't a mutiple of xor key length.");

			int pieces = length / XORKey.Length;

			byte[] outByteArray = new byte[length];

			for (int i = 0; i < pieces; i++)
			{
				for (int pos = 0; pos < XORKey.Length; pos++)
				{
					outByteArray[(i * XORKey.Length) + pos] += (byte)(inByteArray[offsetPos + (i * XORKey.Length) + pos] ^ XORKey[pos]);
				}
			}

			return outByteArray;
		}
	}


}
